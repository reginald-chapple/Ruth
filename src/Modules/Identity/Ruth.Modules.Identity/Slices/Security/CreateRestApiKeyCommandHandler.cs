using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class CreateRestApiKeyCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService
) : ICommandHandler<CreateRestApiKeyCommand, CreateRestApiKeyResponse>
{
    public async Task<Result<CreateRestApiKeyResponse>> HandleAsync(
        CreateRestApiKeyCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = UserAccountId.From(command.UserAccountId);
        var account = await dbContext
            .UserAccounts.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        var prefixEntropy = tokenService.GenerateOpaqueToken(16)[..11];
        var secretEntropy = tokenService.GenerateOpaqueToken(32);
        var plaintextKey = $"ruth_{prefixEntropy}_{secretEntropy}";
        var prefix = plaintextKey[..16];
        var hash = tokenService.HashApiKeyWithSalt(plaintextKey);

        var created = RestApiKey.Create(
            command.Name,
            command.Description,
            prefix,
            hash,
            account.Id,
            command.ExpiresOnUtc
        );
        if (created.IsFailure)
        {
            return Result.Failure<CreateRestApiKeyResponse>(created.Errors);
        }

        var entity = created.Value;
        _ = dbContext.RestApiKeys.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RestApiKeyResponse(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.KeyPrefix,
            entity.UserAccountId.Value,
            account.UserName,
            entity.IsActive,
            entity.ExpiresOnUtc,
            entity.LastUsedOnUtc,
            entity.RevokedOnUtc,
            entity.CreatedOnUtc
        );

        return new CreateRestApiKeyResponse(response, plaintextKey);
    }
}
