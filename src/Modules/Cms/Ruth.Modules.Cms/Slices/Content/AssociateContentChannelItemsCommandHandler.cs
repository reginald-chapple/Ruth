using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class AssociateContentChannelItemsCommandHandler(CmsDbContext db)
    : ICommandHandler<AssociateContentChannelItemsCommand, ContentChannelItemResponse>
{
    public async Task<Result<ContentChannelItemResponse>> HandleAsync(
        AssociateContentChannelItemsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var parentId = ContentChannelItemId.From(command.ParentItemId);
        var childId = ContentChannelItemId.From(command.ChildItemId);

        var parent = await db
            .ContentChannelItems.Include(static i => i.ChildAssociations)
            .FirstOrDefaultAsync(i => i.Id == parentId, cancellationToken);
        if (parent is null)
        {
            return Result.Failure<ContentChannelItemResponse>(
                CmsErrors.NotFound("ContentChannelItem", command.ParentItemId.ToString("D"))
            );
        }

        var childExists = await db
            .ContentChannelItems.AsNoTracking()
            .AnyAsync(i => i.Id == childId, cancellationToken);
        if (!childExists)
        {
            return Result.Failure<ContentChannelItemResponse>(
                CmsErrors.NotFound("ContentChannelItem", command.ChildItemId.ToString("D"))
            );
        }

        var countBefore = parent.ChildAssociations.Count;
        var res = parent.AddAssociation(childId, command.Order);
        if (res.IsFailure)
        {
            return Result.Failure<ContentChannelItemResponse>(res.PrimaryError);
        }

        if (parent.ChildAssociations.Count > countBefore)
        {
            _ = db.ContentChannelItemAssociations.Add(parent.ChildAssociations.Last());
            _ = await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(CmsContentProjectionMapper.ToResponse(parent));
    }
}
