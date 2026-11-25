using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class ConfirmAccountCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<ConfirmAccountCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        ConfirmAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return IdentityErrors.AccountTokenInvalid;
        }

        var utcNow = clock.UtcNow;
        var hash = tokenService.ComputeSha256Hash(command.Token);
        var token = await dbContext.AccountTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash && t.Purpose == AccountTokenPurpose.AccountConfirmation,
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
        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        account.ConfirmAccount();
        token.Consume(utcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
