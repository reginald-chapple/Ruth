using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class ConfirmPasswordResetCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHashingService passwordHashingService,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<ConfirmPasswordResetCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        ConfirmPasswordResetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return IdentityErrors.AccountTokenInvalid;
        }

        var passwordValidation = OnboardingPolicy.ValidatePasswordComplexity(command.NewPassword);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<bool>(passwordValidation.Errors);
        }

        var utcNow = clock.UtcNow;
        var hash = tokenService.ComputeSha256Hash(command.Token);
        var token = await dbContext.AccountTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.Purpose == AccountTokenPurpose.PasswordReset,
            cancellationToken
        );

        if (token is null || !token.IsValidAt(utcNow))
        {
            return IdentityErrors.AccountTokenInvalid;
        }

        var account = await dbContext.UserAccounts.FirstOrDefaultAsync(
            u => u.Id == token.UserAccountId,
            cancellationToken
        );
        if (account is null || !account.IsActive)
        {
            return IdentityErrors.AccountTokenInvalid;
        }

        var newHash = passwordHashingService.HashPassword(command.NewPassword);
        var changeResult = account.ChangePassword(newHash, utcNow);
        if (changeResult.IsFailure)
        {
            return Result.Failure<bool>(changeResult.Errors);
        }

        token.Consume(utcNow);

        var activeRefreshTokens = await dbContext
            .RefreshTokens.Where(r => r.UserAccountId == account.Id && r.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(utcNow, "PasswordReset");
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
