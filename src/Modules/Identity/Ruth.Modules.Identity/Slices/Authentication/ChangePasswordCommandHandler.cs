using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class ChangePasswordCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHashingService passwordHashingService,
    IClock clock
) : ICommandHandler<ChangePasswordCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = UserAccountId.From(command.UserAccountId);
        var account = await dbContext.UserAccounts.FirstOrDefaultAsync(
            u => u.Id == id,
            cancellationToken
        );
        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        if (!passwordHashingService.VerifyPassword(account.PasswordHash, command.CurrentPassword))
        {
            return IdentityErrors.InvalidCredentials;
        }

        var policyResult = OnboardingPolicy.ValidatePasswordComplexity(command.NewPassword);
        if (policyResult.IsFailure)
        {
            return Result.Failure<bool>(policyResult.Errors);
        }

        var utcNow = clock.UtcNow;
        var newHash = passwordHashingService.HashPassword(command.NewPassword);
        var changeResult = account.ChangePassword(newHash, utcNow);
        if (changeResult.IsFailure)
        {
            return Result.Failure<bool>(changeResult.Errors);
        }

        var activeTokens = await dbContext
            .RefreshTokens.Where(t => t.UserAccountId == account.Id && t.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow, "PasswordChanged");
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
