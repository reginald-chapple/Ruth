using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class DeleteTagCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteTagCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteTagCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var tagId = TagId.From(command.TagId);
        var tag = await dbContext.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return CoreCollaborationErrors.TagNotFound;
        }

        if (tag.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        if (!tag.IsAccessibleBy(command.RequestingUserId))
        {
            return CoreCollaborationErrors.TagAccessDenied;
        }

        _ = dbContext.Tags.Remove(tag);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
