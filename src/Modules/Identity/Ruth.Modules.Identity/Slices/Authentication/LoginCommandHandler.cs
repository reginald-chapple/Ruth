using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class LoginCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHashingService passwordHashingService,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<LoginCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var identifier = command.UserNameOrEmail?.Trim() ?? string.Empty;
        var ctx = new LoginAttemptContext(command, identifier, clock.UtcNow);
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(command.Password))
        {
            return await FailAttemptAsync(
                userAccountId: null,
                "MissingCredentials",
                IdentityErrors.InvalidCredentials,
                ctx,
                cancellationToken
            );
        }

        var account = await FindAccountAsync(identifier, cancellationToken);
        return account is null
            ? await FailAttemptAsync(
                userAccountId: null,
                "UserNotFound",
                IdentityErrors.InvalidCredentials,
                ctx,
                cancellationToken
            )
            : await AuthenticateAccountAsync(account, ctx, cancellationToken);
    }

    private Task<UserAccount?> FindAccountAsync(
        string identifier,
        CancellationToken cancellationToken
    )
    {
        var normalized = identifier.ToUpperInvariant();
        return dbContext
            .UserAccounts.Include(static u => u.Roles)
            .FirstOrDefaultAsync(
                u => u.NormalizedUserName == normalized || u.NormalizedEmail == normalized,
                cancellationToken
            );
    }

    private async Task<Result<AuthTokensResponse>> AuthenticateAccountAsync(
        UserAccount account,
        LoginAttemptContext ctx,
        CancellationToken cancellationToken
    )
    {
        var eligibilityError = ValidateAccountEligibility(account, ctx.UtcNow);
        if (eligibilityError is not null)
        {
            return await FailAttemptAsync(
                account.Id,
                eligibilityError.Code,
                eligibilityError,
                ctx,
                cancellationToken
            );
        }

        if (!passwordHashingService.VerifyPassword(account.PasswordHash, ctx.Command.Password))
        {
            account.RecordFailedLogin(ctx.UtcNow);
            var lockedOut = account.IsCurrentlyLockedOut(ctx.UtcNow);
            return await FailAttemptAsync(
                account.Id,
                lockedOut ? "AccountLockedOut" : "InvalidPassword",
                lockedOut ? IdentityErrors.AccountLockedOut : IdentityErrors.InvalidCredentials,
                ctx,
                cancellationToken
            );
        }

        account.RecordSuccessfulLogin(ctx.UtcNow);
        return await CompleteSuccessfulLoginAsync(account, ctx, cancellationToken);
    }

    private async Task<AuthTokensResponse> CompleteSuccessfulLoginAsync(
        UserAccount account,
        LoginAttemptContext ctx,
        CancellationToken cancellationToken
    )
    {
        var tokens = await AuthTokenIssuer.IssueTokensAsync(
            dbContext,
            tokenService,
            account,
            ctx.UtcNow,
            Guid.CreateVersion7(),
            ctx.Command.IpAddress,
            cancellationToken
        );
        _ = dbContext.LoginHistoryEntries.Add(
            LoginHistoryEntry.Record(
                account.Id,
                ctx.Identifier,
                "Password",
                succeeded: true,
                ctx.UtcNow,
                ipAddress: ctx.Command.IpAddress,
                userAgent: ctx.Command.UserAgent
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    private async Task<Result<AuthTokensResponse>> FailAttemptAsync(
        UserAccountId? userAccountId,
        string failureReason,
        DomainError error,
        LoginAttemptContext ctx,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.LoginHistoryEntries.Add(
            LoginHistoryEntry.Record(
                userAccountId,
                ctx.Identifier,
                "Password",
                succeeded: false,
                ctx.UtcNow,
                failureReason: failureReason,
                ipAddress: ctx.Command.IpAddress,
                userAgent: ctx.Command.UserAgent
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return error;
    }

    private static DomainError? ValidateAccountEligibility(
        UserAccount account,
        DateTimeOffset utcNow
    )
    {
        return !account.IsActive ? IdentityErrors.AccountInactive
            : !account.IsConfirmed ? IdentityErrors.AccountNotConfirmed
            : account.IsCurrentlyLockedOut(utcNow) ? IdentityErrors.AccountLockedOut
            : null;
    }

    private readonly record struct LoginAttemptContext(
        LoginCommand Command,
        string Identifier,
        DateTimeOffset UtcNow
    );
}
