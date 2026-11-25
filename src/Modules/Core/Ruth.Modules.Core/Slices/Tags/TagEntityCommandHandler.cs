using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class TagEntityCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<TagEntityCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        TagEntityCommand command,
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
        var exists = await dbContext.TaggedItems.AnyAsync(
            i => i.TagId == tagId && i.EntityTypeId == etId && i.EntityId == command.EntityId,
            cancellationToken
        );
        if (!exists)
        {
            _ = dbContext.TaggedItems.Add(
                new TaggedItem(
                    Guid.CreateVersion7(),
                    tagId,
                    etId,
                    command.EntityId,
                    command.Quantity
                )
            );
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(value: true);
    }
}
