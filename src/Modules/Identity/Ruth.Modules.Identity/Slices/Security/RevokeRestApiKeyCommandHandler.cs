using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class RevokeRestApiKeyCommandHandler(IdentityDbContext dbContext, IClock clock)
    : ICommandHandler<RevokeRestApiKeyCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RevokeRestApiKeyCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var keyId = RestApiKeyId.From(command.RestApiKeyId);
        var key = await dbContext.RestApiKeys.FirstOrDefaultAsync(
            k => k.Id == keyId,
            cancellationToken
        );
        if (key is null)
        {
            return IdentityErrors.ApiKeyNotFound;
        }

        key.Revoke(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
