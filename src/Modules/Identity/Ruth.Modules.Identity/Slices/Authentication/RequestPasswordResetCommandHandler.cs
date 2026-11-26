using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class RequestPasswordResetCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<RequestPasswordResetCommand, IssuedTokenResponse>
{
    public async Task<Result<IssuedTokenResponse>> HandleAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.EmailOrUserName))
        {
            return new IssuedTokenResponse(Issued: false, Token: null, ExpiresOnUtc: null);
        }

        var normalized = command.EmailOrUserName.Trim().ToUpperInvariant();
        var account = await dbContext.UserAccounts.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalized || u.NormalizedUserName == normalized,
            cancellationToken
        );

        if (account is null || !account.IsActive)
        {
            return new IssuedTokenResponse(Issued: false, Token: null, ExpiresOnUtc: null);
        }

        var utcNow = clock.UtcNow;
        var rawToken = tokenService.GenerateOpaqueToken(32);
        var expires = utcNow.ToUniversalTime().Add(AuthenticationPolicy.PasswordResetTokenLifetime);
        var tokenEntity = AccountToken.Create(
            account.Id,
            AccountTokenPurpose.PasswordReset,
            tokenService.ComputeSha256Hash(rawToken),
            utcNow,
            expires
        );

        _ = dbContext.AccountTokens.Add(tokenEntity);
        dbContext.EnqueueIntegrationEvent(
            new PasswordResetRequestedIntegrationEvent(
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
