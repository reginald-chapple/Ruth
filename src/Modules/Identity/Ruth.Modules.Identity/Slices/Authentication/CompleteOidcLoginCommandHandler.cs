using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class CompleteOidcLoginCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IExternalOidcClient oidcClient,
    IClock clock
) : ICommandHandler<CompleteOidcLoginCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        CompleteOidcLoginCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.State) || string.IsNullOrWhiteSpace(command.Code))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var utcNow = clock.UtcNow;
        var key = command.ProviderKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var provider = await dbContext
            .ExternalAuthProviders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderKey == key && p.IsEnabled, cancellationToken);
        if (provider is null)
        {
            return IdentityErrors.ExternalProviderNotFound;
        }

        if (
            command.RedirectUri is not null
            && !SafeRedirectValidator.MatchesExactRedirectUri(
                provider.CallbackPath,
                command.RedirectUri
            )
        )
        {
            return IdentityErrors.RedirectUriMismatch;
        }

        var session = await dbContext.RemoteAuthenticationSessions.FirstOrDefaultAsync(
            s => s.ProviderKey == key && s.State == command.State,
            cancellationToken
        );
        if (!IsValidSessionWithConstantTimeState(session, command.State, utcNow))
        {
            return IdentityErrors.RemoteAuthSessionInvalid;
        }

        var userInfoResult = await oidcClient.ExchangeCodeAsync(
            provider,
            command.Code,
            session!.CodeVerifier,
            session.Nonce,
            cancellationToken
        );
        if (userInfoResult.IsFailure)
        {
            return Result.Failure<AuthTokensResponse>(userInfoResult.Errors);
        }

        session.Complete(utcNow);
        return await FinalizeOidcSignInAsync(
            provider,
            userInfoResult.Value,
            command,
            utcNow,
            cancellationToken
        );
    }

    private async Task<Result<AuthTokensResponse>> FinalizeOidcSignInAsync(
        ExternalAuthProviderConfig provider,
        ExternalOidcUserInfo userInfo,
        CompleteOidcLoginCommand command,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var account = await ResolveOrLinkAccountAsync(
            provider,
            userInfo,
            utcNow,
            cancellationToken
        );
        if (account is null)
        {
            _ = dbContext.LoginHistoryEntries.Add(
                LoginHistoryEntry.Record(
                    userAccountId: null,
                    userInfo.Email ?? userInfo.Subject,
                    "Oidc",
                    succeeded: false,
                    utcNow,
                    providerKey: provider.ProviderKey,
                    failureReason: "ExternalLoginUnlinked",
                    ipAddress: command.IpAddress,
                    userAgent: command.UserAgent
                )
            );
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return IdentityErrors.ExternalLoginUnlinked;
        }

        if (!account.IsActive || account.IsCurrentlyLockedOut(utcNow))
        {
            return IdentityErrors.AccountInactive;
        }

        account.RecordSuccessfulLogin(utcNow);
        var tokens = await AuthTokenIssuer.IssueTokensAsync(
            dbContext,
            tokenService,
            account,
            utcNow,
            Guid.CreateVersion7(),
            command.IpAddress,
            cancellationToken
        );
        _ = dbContext.LoginHistoryEntries.Add(
            LoginHistoryEntry.Record(
                account.Id,
                account.UserName,
                "Oidc",
                succeeded: true,
                utcNow,
                providerKey: provider.ProviderKey,
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    private async Task<UserAccount?> ResolveOrLinkAccountAsync(
        ExternalAuthProviderConfig provider,
        ExternalOidcUserInfo userInfo,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var link = await dbContext.ExternalLoginLinks.FirstOrDefaultAsync(
            l => l.ProviderKey == provider.ProviderKey && l.ProviderSubject == userInfo.Subject,
            cancellationToken
        );
        if (link is not null)
        {
            return await dbContext
                .UserAccounts.Include(static u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == link.UserAccountId, cancellationToken);
        }

        if (
            !provider.AutoLinkByEmail
            || !userInfo.EmailVerified
            || string.IsNullOrWhiteSpace(userInfo.Email)
        )
        {
            return null;
        }

        var normalizedEmail = userInfo.Email.Trim().ToUpperInvariant();
        var matchedAccount = await dbContext
            .UserAccounts.Include(static u => u.Roles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (matchedAccount is null)
        {
            return null;
        }

        _ = dbContext.ExternalLoginLinks.Add(
            ExternalLoginLink.Create(
                matchedAccount.Id,
                provider.ProviderKey,
                userInfo.Subject,
                userInfo.Email,
                utcNow
            )
        );
        return matchedAccount;
    }

    private static bool IsValidSessionWithConstantTimeState(
        RemoteAuthenticationSession? session,
        string candidateState,
        DateTimeOffset utcNow
    )
    {
        return session is not null
            && session.IsValidAt(utcNow)
            && !string.IsNullOrWhiteSpace(session.CodeVerifier)
            && !string.IsNullOrWhiteSpace(session.Nonce)
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.State),
                Encoding.UTF8.GetBytes(candidateState)
            );
    }
}
