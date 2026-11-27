using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ruth.Modules.Tenancy.Tests;

public sealed class TenancyLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("ruth_tenancy_test")
        .WithUsername("ruth")
        .WithPassword("ruth_test_pw")
        .Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Full_TaxExempt_Lifecycle_Enforces_Irs_EoBmf_Mock_StripeConnect_CustomDomain_And_PlatformSchema_Isolation()
    {
        var seededTenants = new List<TenantId>();
        await using var provider = await TenancyTestHostFactory.BuildMigratedProviderAsync(
            _postgres,
            seededTenants
        );

        var commands = provider.GetRequiredService<ICommandDispatcher>();
        var hostResolver = provider.GetRequiredService<ITenantHostResolver>();

        await AssertNonExemptSocialClubRejectedAsync(commands);

        var tenantId = await ProvisionHelpingHandsSandboxAsync(commands, seededTenants);
        await AssertInitialHostRoutingAsync(hostResolver, tenantId);
        await CompleteComplianceAndSubmitForGoLiveAsync(commands, hostResolver, tenantId);
        await ApproveGoLiveVerifyCustomDomainAndSuspendAsync(provider, tenantId);

        await TenancyTestHostFactory.AssertAppRoleCannotReadPlatformSchemaAsync(provider);
    }

    [Fact]
    public async Task StripeConnectAccount_Persists_In_Platform_Schema_And_ManualReview_Override_And_Operator_Guard_Work()
    {
        await using var provider = await TenancyTestHostFactory.BuildMigratedProviderAsync(
            _postgres,
            []
        );
        var commands = provider.GetRequiredService<ICommandDispatcher>();

        // EIN ending in 7777 triggers IrsVerificationStatus.ManualReviewRequired
        var signup = await commands.SendAsync<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse
        >(
            new SignupTaxExemptTenantCommand(
                "Newly Formed Relief Trust",
                "Relief Trust",
                "relief-trust",
                "52-1237777",
                "501(c)(3)",
                "VA",
                "UTC",
                "Main",
                "trustadmin",
                "admin@relieftrust.org",
                "Trust Admin",
                "StrongPass!2026#"
            )
        );
        Assert.True(signup.IsSuccess);
        var tenantId = TenantId.From(signup.Value.TenantId);

        using (
            AmbientTenantContext.BeginScope(tenantId, "relief-trust", TenantLifecycleStage.Sandbox)
        )
        {
            var stripeDone = await commands.SendAsync<
                CompleteMockStripeConnectCommand,
                TenantComplianceStatusResponse
            >(new CompleteMockStripeConnectCommand());
            Assert.True(stripeDone.IsSuccess);

            var submitted = await commands.SendAsync<
                SubmitTenantForGoLiveCommand,
                TenantComplianceStatusResponse
            >(new SubmitTenantForGoLiveCommand());
            Assert.True(submitted.IsSuccess);
            Assert.Equal(IrsVerificationStatus.ManualReviewRequired, submitted.Value.IrsStatus);
        }

        await TenancyTestHostFactory.AssertPersistedStripeAndIrsRowsAsync(provider, tenantId);

        using (AmbientTenantContext.BeginPlatformOperatorScope())
        {
            // Unregistered operator email MUST be rejected
            var spoofAttempt = await commands.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId.Value,
                    OperatorReviewDecision.ApproveLive,
                    "forged@attacker.example",
                    "Spoofed approval",
                    ManualIrsOverrideApproved: true
                )
            );
            Assert.True(spoofAttempt.IsFailure);
            Assert.Equal("Tenancy.InvalidOperatorIdentity", spoofAttempt.PrimaryError.Code);

            // Real active operator with ManualIrsOverrideApproved = true approves Live
            var approved = await commands.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId.Value,
                    OperatorReviewDecision.ApproveLive,
                    "compliance@ops.localhost",
                    "Determination letter inspected",
                    ManualIrsOverrideApproved: true
                )
            );
            Assert.True(approved.IsSuccess);
            Assert.Equal(TenantLifecycleStage.Live, approved.Value.LifecycleStage);
        }
    }

    [Fact]
    public async Task Revoked_Irs_Ein_Is_Rejected_On_GoLive_Submission()
    {
        await using var provider = await TenancyTestHostFactory.BuildMigratedProviderAsync(
            _postgres,
            []
        );
        var commands = provider.GetRequiredService<ICommandDispatcher>();

        var signup = await commands.SendAsync<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse
        >(
            new SignupTaxExemptTenantCommand(
                "Revoked Foundation",
                "Revoked Foundation",
                "revoked-fdn",
                "52-1239999",
                "501(c)(3)",
                "DE",
                "UTC",
                "Main",
                "fdnadmin",
                "admin@revoked.example",
                "Admin",
                "StrongPass!2026#"
            )
        );
        Assert.True(signup.IsSuccess);
        var tenantId = TenantId.From(signup.Value.TenantId);

        using (
            AmbientTenantContext.BeginScope(tenantId, "revoked-fdn", TenantLifecycleStage.Sandbox)
        )
        {
            _ = await commands.SendAsync<
                CompleteMockStripeConnectCommand,
                TenantComplianceStatusResponse
            >(new CompleteMockStripeConnectCommand());
            var submit = await commands.SendAsync<
                SubmitTenantForGoLiveCommand,
                TenantComplianceStatusResponse
            >(new SubmitTenantForGoLiveCommand());
            Assert.True(submit.IsFailure);
            Assert.Equal("Tenancy.IrsVerificationFailed", submit.Errors[0].Code);
        }
    }

    private static async Task AssertNonExemptSocialClubRejectedAsync(ICommandDispatcher commands)
    {
        var clubAttempt = await commands.SendAsync<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse
        >(
            new SignupTaxExemptTenantCommand(
                "Downtown Social Club",
                "Downtown Club",
                "downtown-club",
                "52-1112223",
                "501(c)(7)",
                "VA",
                "America/New_York",
                "Main",
                "clubadmin",
                "admin@club.example",
                "Club Admin",
                "StrongPass!2026#"
            )
        );
        Assert.True(clubAttempt.IsFailure);
        Assert.Contains(clubAttempt.Errors, static e =>
            string.Equals(e.Code, "Tenancy.IneligibleSubsection", StringComparison.Ordinal)
        );
    }

    private static async Task<TenantId> ProvisionHelpingHandsSandboxAsync(
        ICommandDispatcher commands,
        List<TenantId> seededTenants
    )
    {
        var signupResult = await commands.SendAsync<
            SignupTaxExemptTenantCommand,
            SignupTaxExemptTenantResponse
        >(
            new SignupTaxExemptTenantCommand(
                "Helping Hands Community Relief Inc",
                "Helping Hands",
                "helping-hands",
                "52-1234567",
                "501(c)(3)",
                "MD",
                "America/New_York",
                "Baltimore Center",
                "reliefadmin",
                "ops@helpinghands.org",
                "Relief Director",
                "StrongPass!2026#"
            )
        );
        Assert.True(signupResult.IsSuccess);
        var tenantId = TenantId.From(signupResult.Value.TenantId);
        Assert.Equal("helping-hands.sandbox.localhost", signupResult.Value.SandboxHostname);
        Assert.Contains(tenantId, seededTenants);
        return tenantId;
    }

    private static async Task AssertInitialHostRoutingAsync(
        ITenantHostResolver hostResolver,
        TenantId tenantId
    )
    {
        Assert.Equal(
            ResolvedHostKind.PlatformOperator,
            (await hostResolver.ResolveHostAsync("ops.localhost:8080")).Kind
        );
        Assert.Equal(
            ResolvedHostKind.PlatformSignup,
            (await hostResolver.ResolveHostAsync("localhost:5000")).Kind
        );

        var sandboxHost = await hostResolver.ResolveHostAsync("helping-hands.sandbox.localhost");
        Assert.Equal(ResolvedHostKind.TenantWorkspace, sandboxHost.Kind);
        Assert.Equal(tenantId, sandboxHost.TenantId);
        Assert.Equal(TenantLifecycleStage.Sandbox, sandboxHost.LifecycleStage);

        Assert.Equal(
            ResolvedHostKind.Unknown,
            (await hostResolver.ResolveHostAsync("unregistered.charity.org")).Kind
        );
    }

    private static async Task CompleteComplianceAndSubmitForGoLiveAsync(
        ICommandDispatcher commands,
        ITenantHostResolver hostResolver,
        TenantId tenantId
    )
    {
        using (
            AmbientTenantContext.BeginScope(tenantId, "helping-hands", TenantLifecycleStage.Sandbox)
        )
        {
            var prematureSubmit = await commands.SendAsync<
                SubmitTenantForGoLiveCommand,
                TenantComplianceStatusResponse
            >(new SubmitTenantForGoLiveCommand());
            Assert.True(prematureSubmit.IsFailure);
            Assert.Equal("Tenancy.StripeConnectIncomplete", prematureSubmit.Errors[0].Code);

            var domainReg = await commands.SendAsync<
                RegisterCustomDomainCommand,
                TenantDomainResponse
            >(
                new RegisterCustomDomainCommand(
                    "giving.helpinghands.org",
                    MakePrimaryWhenVerified: true
                )
            );
            Assert.True(domainReg.IsSuccess);

            var domainVerify = await commands.SendAsync<
                VerifyCustomDomainCommand,
                TenantDomainResponse
            >(
                new VerifyCustomDomainCommand(
                    domainReg.Value.Id,
                    domainReg.Value.VerificationTxtValue
                )
            );
            Assert.True(domainVerify.IsSuccess);
            Assert.True(domainVerify.Value.IsVerified);

            Assert.Equal(
                ResolvedHostKind.Unknown,
                (await hostResolver.ResolveHostAsync("giving.helpinghands.org")).Kind
            );

            var mockStripe = await commands.SendAsync<
                CompleteMockStripeConnectCommand,
                TenantComplianceStatusResponse
            >(new CompleteMockStripeConnectCommand());
            Assert.True(mockStripe.IsSuccess);
            Assert.True(mockStripe.Value.StripeChargesEnabled);
            Assert.True(mockStripe.Value.StripePayoutsEnabled);

            var submitResult = await commands.SendAsync<
                SubmitTenantForGoLiveCommand,
                TenantComplianceStatusResponse
            >(new SubmitTenantForGoLiveCommand());
            Assert.True(submitResult.IsSuccess);
            Assert.Equal(
                TenantLifecycleStage.PendingVerification,
                submitResult.Value.LifecycleStage
            );
            Assert.Equal(IrsVerificationStatus.VerifiedActiveExempt, submitResult.Value.IrsStatus);
        }
    }

    private static async Task ApproveGoLiveVerifyCustomDomainAndSuspendAsync(
        ServiceProvider provider,
        TenantId tenantId
    )
    {
        var commands = provider.GetRequiredService<ICommandDispatcher>();
        var queries = provider.GetRequiredService<IQueryDispatcher>();
        var hostResolver = provider.GetRequiredService<ITenantHostResolver>();
        var activeRegistry = provider.GetRequiredService<IActiveTenantRegistry>();

        using (AmbientTenantContext.BeginPlatformOperatorScope())
        {
            var opAuth = await commands.SendAsync<
                AuthenticatePlatformOperatorCommand,
                PlatformOperatorAuthResponse
            >(
                new AuthenticatePlatformOperatorCommand(
                    "compliance@ops.localhost",
                    "OperatorPass!2026#"
                )
            );
            Assert.True(opAuth.IsSuccess);

            var pendingQueue = await queries.QueryAsync<
                GetPendingGoLiveQueueQuery,
                IReadOnlyList<TenantSummaryResponse>
            >(new GetPendingGoLiveQueueQuery(TenantLifecycleStage.PendingVerification));
            Assert.True(pendingQueue.IsSuccess);
            _ = Assert.Single(pendingQueue.Value);

            var approval = await commands.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId.Value,
                    OperatorReviewDecision.ApproveLive,
                    opAuth.Value.Email,
                    "Verified 501(c)(3) status in IRS EO BMF."
                )
            );
            Assert.True(approval.IsSuccess);
            Assert.Equal(TenantLifecycleStage.Live, approval.Value.LifecycleStage);

            var liveCustom = await hostResolver.ResolveHostAsync("giving.helpinghands.org");
            Assert.Equal(ResolvedHostKind.TenantWorkspace, liveCustom.Kind);
            Assert.Equal(tenantId, liveCustom.TenantId);
            Assert.Equal(TenantLifecycleStage.Live, liveCustom.LifecycleStage);

            var suspended = await commands.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId.Value,
                    OperatorReviewDecision.Suspend,
                    opAuth.Value.Email,
                    "Compliance hold"
                )
            );
            Assert.True(suspended.IsSuccess);
            Assert.Equal(
                ResolvedHostKind.Unknown,
                (await hostResolver.ResolveHostAsync("giving.helpinghands.org")).Kind
            );
            Assert.DoesNotContain(tenantId, await activeRegistry.GetActiveTenantIdsAsync());
        }
    }
}
