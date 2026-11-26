using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class RequestAccountConfirmationCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<RequestAccountConfirmationCommand, IssuedTokenResponse>
{
    public async Task<Result<IssuedTokenResponse>> HandleAsync(
        RequestAccountConfirmationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var accountId = UserAccountId.From(command.UserAccountId);
        var account = await dbContext.UserAccounts.FirstOrDefaultAsync(
            u => u.Id == accountId,
            cancellationToken
        );
        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        var utcNow = clock.UtcNow;
        var rawToken = tokenService.GenerateOpaqueToken(32);
        var expires = utcNow
            .ToUniversalTime()
            .Add(AuthenticationPolicy.AccountConfirmationTokenLifetime);
        var tokenEntity = AccountToken.Create(
            account.Id,
            AccountTokenPurpose.AccountConfirmation,
            tokenService.ComputeSha256Hash(rawToken),
            utcNow,
            expires
        );

        _ = dbContext.AccountTokens.Add(tokenEntity);
        dbContext.EnqueueIntegrationEvent(
            new AccountConfirmationRequestedIntegrationEvent(
                account.Id.Value,
                account.Email,
                account.UserName,
                rawToken,
                expires,
                utcNow
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new IssuedTokenResponse(Issued: true, rawToken, expires);
    }
}
