using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Preferences;

internal sealed class DeletePersonPreferenceCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeletePersonPreferenceCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeletePersonPreferenceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedKey = command.Key.Trim();
        var existing = await dbContext.PersonPreferences.FirstOrDefaultAsync(
            p => p.OwnerKey == command.OwnerKey && p.Key == normalizedKey,
            cancellationToken
        );
        if (existing is null)
        {
            return CoreCollaborationErrors.PreferenceNotFound;
        }

        _ = dbContext.PersonPreferences.Remove(existing);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
