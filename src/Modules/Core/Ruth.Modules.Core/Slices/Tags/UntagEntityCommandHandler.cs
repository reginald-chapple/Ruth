using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class UntagEntityCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UntagEntityCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        UntagEntityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var tagId = TagId.From(command.TagId);
        var tag = await dbContext
            .Tags.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return CoreCollaborationErrors.TagNotFound;
        }

        if (!tag.IsAccessibleBy(command.RequestingUserId))
        {
            return CoreCollaborationErrors.TagAccessDenied;
        }

        var etId = EntityTypeDescriptorId.From(command.EntityTypeId);
        var item = await dbContext.TaggedItems.FirstOrDefaultAsync(
            i => i.TagId == tagId && i.EntityTypeId == etId && i.EntityId == command.EntityId,
            cancellationToken
        );
        if (item is not null)
        {
            _ = dbContext.TaggedItems.Remove(item);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(value: true);
    }
}
