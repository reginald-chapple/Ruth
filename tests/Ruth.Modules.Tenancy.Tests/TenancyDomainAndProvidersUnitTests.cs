using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Domain.Events;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.Modules.Tenancy.Infrastructure.Security;
using Ruth.Modules.Tenancy.Infrastructure.Verification;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Modules.Tenancy.Tests;

public sealed class TenancyDomainAndProvidersUnitTests
{
    [Theory]
    [InlineData("52-1234567", "52-1234567")]
    [InlineData("52 1234567", "52-1234567")]
    [InlineData("521234567", "52-1234567")]
    public void EinFormatter_Normalizes_Valid_Irs_Numbers(string raw, string expected)
    {
        var result = EinFormatter.NormalizeAndValidate(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("00-1234567")]
    [InlineData("07-1234567")]
    [InlineData("52-0000000")]
    [InlineData("52-123456")]
    [InlineData("52-12345678")]
    [InlineData("AB-1234567")]
    public void EinFormatter_Rejects_Invalid_Prefixes_Lengths_And_Zero_Serials(string raw)
    {
        var result = EinFormatter.NormalizeAndValidate(raw);
        Assert.True(result.IsFailure);
        Assert.Equal("Tenancy.InvalidEin", result.PrimaryError.Code);
    }

    [Fact]
    public void TenantSlugPolicy_Enforces_Reserved_Slugs_StateCodes_Subsections_TimeZones_And_OnboardingUrls()
    {
        Assert.True(TenantSlugPolicy.NormalizeAndValidateSlug("ops").IsFailure);
        Assert.True(TenantSlugPolicy.NormalizeAndValidateSlug("admin").IsFailure);
        Assert.True(TenantSlugPolicy.NormalizeAndValidateSlug("-bad").IsFailure);
        Assert.True(TenantSlugPolicy.NormalizeAndValidateSlug("bad--slug").IsFailure);
        Assert.Equal("harbor-aid", TenantSlugPolicy.NormalizeAndValidateSlug("Harbor-Aid").Value);

        Assert.Equal("501(c)(3)", TenantSlugPolicy.NormalizeAndValidateSubsection("501c3").Value);
        Assert.True(TenantSlugPolicy.NormalizeAndValidateSubsection("501(c)(7)").IsFailure);

        Assert.Equal("MD", TenantSlugPolicy.NormalizeAndValidateStateCode("md").Value);
        Assert.Equal("DC", TenantSlugPolicy.NormalizeAndValidateStateCode("dc").Value);
        Assert.True(TenantSlugPolicy.NormalizeAndValidateStateCode("ZZ").IsFailure);

        Assert.Equal(
            "UTC",
            TenantSlugPolicy.NormalizeAndValidateTimeZoneId(rawTimeZoneId: null).Value
        );
        Assert.True(TenantSlugPolicy.NormalizeAndValidateTimeZoneId("Mars/Olympus_Mons").IsFailure);

        Assert.Equal(
            "/admin/compliance",
            TenantSlugPolicy.NormalizeAndValidateOnboardingUrl("/admin/compliance").Value
        );
        Assert.Equal(
            "https://giving.org/done",
            TenantSlugPolicy.NormalizeAndValidateOnboardingUrl("https://giving.org/done").Value
        );
        Assert.True(
            TenantSlugPolicy.NormalizeAndValidateOnboardingUrl("javascript:alert(1)").IsFailure
        );
        Assert.True(TenantSlugPolicy.NormalizeAndValidateOnboardingUrl("//evil.example").IsFailure);

        Assert.Equal(
            "donate.harboraid.org",
            TenantSlugPolicy
                .NormalizeAndValidateHostname("Donate.HarborAid.org:8443", "sandbox.localhost")
                .Value
        );
        Assert.True(
            TenantSlugPolicy
                .NormalizeAndValidateHostname("localhost", "sandbox.localhost")
                .IsFailure
        );
        Assert.True(
            TenantSlugPolicy
                .NormalizeAndValidateHostname("sub.sandbox.localhost", "sandbox.localhost")
                .IsFailure
        );
    }

    [Fact]
    public void Tenant_Lifecycle_StateMachine_Enforces_Transitions_And_Emits_DomainEvents()
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var reg = Tenant.RegisterSandbox(
            "Community Food Bank Inc",
            "Community Food Bank",
            "community-food-bank",
            "52-1234567",
            "501(c)(3)",
            "MD",
            "UTC",
            now
        );
        Assert.True(reg.IsSuccess);
        var tenant = reg.Value;
        Assert.Contains(tenant.DomainEvents, static e => e is TenantSandboxRegisteredDomainEvent);

        // Cannot approve Live directly from Sandbox before submitting
        Assert.Equal(
            "Tenancy.InvalidLifecycleTransition",
            tenant
                .ApproveLive("op@ops.localhost", "premature", manualIrsOverrideApproved: false, now)
                .PrimaryError.Code
        );

        Assert.True(
            tenant
                .AttachStripeConnectAccount(
                    "acct_mock_123",
                    chargesEnabled: true,
                    payoutsEnabled: true
                )
                .IsSuccess
        );
        tenant.RecordIrsVerification(
            IrsVerificationStatus.ManualReviewRequired,
            officialName: null,
            deductibilityCode: null,
            now
        );

        Assert.True(tenant.SubmitForGoLive(now).IsSuccess);
        Assert.Equal(TenantLifecycleStage.PendingVerification, tenant.LifecycleStage);
        Assert.Contains(tenant.DomainEvents, static e => e is TenantSubmittedForGoLiveDomainEvent);

        // ManualReviewRequired without override fails ApproveLive
        Assert.Equal(
            "Tenancy.IrsVerificationFailed",
            tenant
                .ApproveLive(
                    "op@ops.localhost",
                    "missing override",
                    manualIrsOverrideApproved: false,
                    now
                )
                .PrimaryError.Code
        );

        // ManualReviewRequired WITH override succeeds and promotes to Live
        Assert.True(
            tenant
                .ApproveLive(
                    "op@ops.localhost",
                    "Determination letter verified",
                    manualIrsOverrideApproved: true,
                    now
                )
                .IsSuccess
        );
        Assert.Equal(TenantLifecycleStage.Live, tenant.LifecycleStage);
        Assert.Equal(IrsVerificationStatus.VerifiedActiveExempt, tenant.IrsStatus);
        Assert.Contains(tenant.DomainEvents, static e => e is TenantApprovedLiveDomainEvent);

        // ReturnToSandbox clears approval metadata and emits event
        Assert.True(tenant.ReturnToSandbox("op@ops.localhost", "Recheck domain", now).IsSuccess);
        Assert.Equal(TenantLifecycleStage.Sandbox, tenant.LifecycleStage);
        Assert.Null(tenant.ApprovedOnUtc);
        Assert.Contains(tenant.DomainEvents, static e => e is TenantReturnedToSandboxDomainEvent);

        // Suspend transitions to Suspended and blocks duplicate suspension
        Assert.True(tenant.Suspend("op@ops.localhost", "Hold", now).IsSuccess);
        Assert.Equal(TenantLifecycleStage.Suspended, tenant.LifecycleStage);
        Assert.Contains(tenant.DomainEvents, static e => e is TenantSuspendedDomainEvent);
        Assert.True(tenant.Suspend("op@ops.localhost", "Again", now).IsFailure);
    }

    [Fact]
    public async Task Providers_Enforce_MockMode_Guards_And_Operator_Hasher_Verifies_RoundTrip()
    {
        var liveOpts = Options.Create(
            new TenancyOptions { StripeMockMode = false, DnsMockMode = false }
        );

        var stripe = new MockStripeConnectProvider(liveOpts, new SystemClock());
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stripe.CompleteMockOnboardingAsync("acct_123")
        );

        var dns = new SimulatedDnsTxtVerificationProvider(liveOpts);
        var unverified = await dns.ResolveTxtRecordsAsync(
            "_org-verify.giving.org",
            simulatedObservedToken: "domain-verify-forged"
        );
        Assert.Empty(unverified);

        dns.StageTxtRecord("_org-verify.giving.org", "domain-verify-real");
        var staged = await dns.ResolveTxtRecordsAsync(
            "_org-verify.giving.org",
            simulatedObservedToken: "domain-verify-forged"
        );
        var single = Assert.Single(staged);
        Assert.Equal("domain-verify-real", single);

        var hash = PlatformOperatorPasswordHasher.HashPassword("OperatorPass!2026#");
        Assert.True(PlatformOperatorPasswordHasher.VerifyPassword("OperatorPass!2026#", hash));
        Assert.False(PlatformOperatorPasswordHasher.VerifyPassword("WrongPassword!", hash));
    }
}
