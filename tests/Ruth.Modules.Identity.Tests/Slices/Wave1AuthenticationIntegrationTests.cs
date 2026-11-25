using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Slices;

[Collection(IdentityPostgresFixtures.Name)]
public sealed class Wave1AuthenticationIntegrationTests(Postgres18ContainerFixture fixture)
{
    [Fact]
    public async Task Login_RefreshRotation_ReuseRevokesFamily_AndRecordsHistory()
    {
        var connectionString = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            connectionString,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var onboard = await commands.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(IdentityTestHostFactory.CreateValidCommand());
        Assert.True(onboard.IsSuccess);

        var login = await commands.SendAsync<LoginCommand, AuthTokensResponse>(
            new LoginCommand("admin", "StrongPass#2026!", "127.0.0.1", "TestAgent")
        );
        Assert.True(login.IsSuccess);
        Assert.NotEmpty(login.Value.AccessToken);
        Assert.Contains(
            SecurityRole.AdministratorsRoleName,
            login.Value.Roles,
            StringComparer.Ordinal
        );

        var rotated = await commands.SendAsync<RefreshAccessTokenCommand, AuthTokensResponse>(
            new RefreshAccessTokenCommand(login.Value.RefreshToken, "127.0.0.1", "TestAgent")
        );
        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(
            login.Value.RefreshToken,
            rotated.Value.RefreshToken,
            StringComparer.Ordinal
        );

        // Reusing the already-rotated refresh token must fail AND revoke the rotated descendant!
        var reuse = await commands.SendAsync<RefreshAccessTokenCommand, AuthTokensResponse>(
            new RefreshAccessTokenCommand(login.Value.RefreshToken)
        );
        Assert.True(reuse.IsFailure);

        var afterReuse = await commands.SendAsync<RefreshAccessTokenCommand, AuthTokensResponse>(
            new RefreshAccessTokenCommand(rotated.Value.RefreshToken)
        );
        Assert.True(afterReuse.IsFailure);

        var history = await queries.QueryAsync<
            GetLoginHistoryQuery,
            IReadOnlyList<LoginHistoryItemResponse>
        >(new GetLoginHistoryQuery(login.Value.UserAccountId, MaxCount: 20));
        Assert.True(history.IsSuccess);
        Assert.True(history.Value.Count >= 2);
    }

    [Fact]
    public async Task Lockout_PasswordReset_AndAccountConfirmationFlows_WorkEndToEnd()
    {
        var connectionString = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            connectionString,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        _ = await commands.SendAsync<CompleteOnboardingCommand, CompleteOnboardingResponse>(
            IdentityTestHostFactory.CreateValidCommand()
        );

        var created = await commands.SendAsync<
            CreateUserAccountCommand,
            UserAccountSummaryResponse
        >(
            new CreateUserAccountCommand(
                "staffuser",
                "staff@grace.org",
                "Staff User",
                "StaffPass#2026!",
                IsConfirmed: false
            )
        );
        Assert.True(created.IsSuccess);

        var unconfirmedLogin = await commands.SendAsync<LoginCommand, AuthTokensResponse>(
            new LoginCommand("staffuser", "StaffPass#2026!")
        );
        Assert.True(unconfirmedLogin.IsFailure);
        Assert.Equal("Identity.AccountNotConfirmed", unconfirmedLogin.PrimaryError.Code);

        var confirmToken = await commands.SendAsync<
            RequestAccountConfirmationCommand,
            IssuedTokenResponse
        >(new RequestAccountConfirmationCommand(created.Value.UserAccountId));
        Assert.True(confirmToken.IsSuccess);
        Assert.NotNull(confirmToken.Value.Token);

        var confirmed = await commands.SendAsync<ConfirmAccountCommand, bool>(
            new ConfirmAccountCommand(confirmToken.Value.Token!)
        );
        Assert.True(confirmed.IsSuccess);

        var reuseConfirm = await commands.SendAsync<ConfirmAccountCommand, bool>(
            new ConfirmAccountCommand(confirmToken.Value.Token!)
        );
        Assert.True(reuseConfirm.IsFailure);

        for (var i = 0; i < AuthenticationPolicy.MaxFailedPasswordAttempts; i++)
        {
            var failed = await commands.SendAsync<LoginCommand, AuthTokensResponse>(
                new LoginCommand("staffuser", "WrongPass#9999!")
            );
            Assert.True(failed.IsFailure);
        }

        var locked = await commands.SendAsync<LoginCommand, AuthTokensResponse>(
            new LoginCommand("staffuser", "StaffPass#2026!")
        );
        Assert.True(locked.IsFailure);
        Assert.Equal("Identity.AccountLockedOut", locked.PrimaryError.Code);

        var resetIssued = await commands.SendAsync<
            RequestPasswordResetCommand,
            IssuedTokenResponse
        >(new RequestPasswordResetCommand("staff@grace.org"));
        Assert.True(resetIssued.IsSuccess);
        Assert.True(resetIssued.Value.Issued);

        var resetConfirm = await commands.SendAsync<ConfirmPasswordResetCommand, bool>(
            new ConfirmPasswordResetCommand(resetIssued.Value.Token!, "ResetPass#2026!")
        );
        Assert.True(resetConfirm.IsSuccess);

        var okLogin = await commands.SendAsync<LoginCommand, AuthTokensResponse>(
            new LoginCommand("staff@grace.org", "ResetPass#2026!")
        );
        Assert.True(okLogin.IsSuccess);

        var changed = await commands.SendAsync<ChangePasswordCommand, bool>(
            new ChangePasswordCommand(
                created.Value.UserAccountId,
                "ResetPass#2026!",
                "ChangedPass#2026!"
            )
        );
        Assert.True(changed.IsSuccess);

        var oldRefresh = await commands.SendAsync<RefreshAccessTokenCommand, AuthTokensResponse>(
            new RefreshAccessTokenCommand(okLogin.Value.RefreshToken)
        );
        Assert.True(oldRefresh.IsFailure);
    }

    [Fact]
    public async Task ExternalOidcFlow_StartsPkceSession_AutoLinksVerifiedEmail_AndIssuesTokens()
    {
        var connectionString = await fixture.CreateIsolatedDatabaseAsync();
        var fakeOidc = new FakeExternalOidcClient(
            new ExternalOidcUserInfo(
                "ext-sub-123",
                "admin@grace.org",
                EmailVerified: true,
                "Grace Admin"
            )
        );

        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            connectionString,
            autoMigrate: true,
            configureServices: services =>
            {
                _ = services.AddScoped<IExternalOidcClient>(_ => fakeOidc);
            }
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        _ = await commands.SendAsync<CompleteOnboardingCommand, CompleteOnboardingResponse>(
            IdentityTestHostFactory.CreateValidCommand()
        );

        var upsert = await commands.SendAsync<
            UpsertExternalAuthProviderCommand,
            ExternalAuthProviderResponse
        >(
            new UpsertExternalAuthProviderCommand(
                "microsoft",
                "Microsoft Entra ID",
                "https://login.microsoftonline.com/common/v2.0",
                "client-123",
                "secret-xyz",
                "openid profile email",
                IsEnabled: true,
                AutoLinkByEmail: true,
                "https://localhost:5001/signin-oidc"
            )
        );
        Assert.True(upsert.IsSuccess);

        var started = await commands.SendAsync<StartOidcLoginCommand, StartOidcLoginResponse>(
            new StartOidcLoginCommand("microsoft", "/dashboard")
        );
        Assert.True(started.IsSuccess);
        Assert.Contains(
            "code_challenge=",
            started.Value.AuthorizationUrl,
            StringComparison.Ordinal
        );

        var completed = await commands.SendAsync<CompleteOidcLoginCommand, AuthTokensResponse>(
            new CompleteOidcLoginCommand("microsoft", started.Value.State, "auth-code-1")
        );
        Assert.True(completed.IsSuccess);
        Assert.Equal("admin", completed.Value.UserName);

        var replay = await commands.SendAsync<CompleteOidcLoginCommand, AuthTokensResponse>(
            new CompleteOidcLoginCommand("microsoft", started.Value.State, "auth-code-1")
        );
        Assert.True(replay.IsFailure);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(1, await db.ExternalLoginLinks.CountAsync());
    }

    private sealed class FakeExternalOidcClient(ExternalOidcUserInfo userInfo) : IExternalOidcClient
    {
        public Task<Result<string>> BuildAuthorizationUrlAsync(
            ExternalAuthProviderConfig provider,
            string state,
            string nonce,
            string codeChallenge,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                Result.Success(
                    $"{provider.Authority}/authorize?state={state}&nonce={nonce}&code_challenge={codeChallenge}"
                )
            );
        }

        public Task<Result<ExternalOidcUserInfo>> ExchangeCodeAsync(
            ExternalAuthProviderConfig provider,
            string code,
            string codeVerifier,
            string expectedNonce,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Result.Success(userInfo));
        }
    }
}
