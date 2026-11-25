using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class GetEntityTagsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetEntityTagsQuery, IReadOnlyList<TagResponse>>
{
    public async Task<Result<IReadOnlyList<TagResponse>>> HandleAsync(
        GetEntityTagsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var etId = EntityTypeDescriptorId.From(query.EntityTypeId);
        var tagIds = await dbContext
            .TaggedItems.AsNoTracking()
            .Where(i => i.EntityTypeId == etId && i.EntityId == query.EntityId)
            .Select(static i => i.TagId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (tagIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<TagResponse>>([]);
        }

        var viewer = query.ViewerUserId;
        var tags = await dbContext
            .Tags.AsNoTracking()
            .Include(static t => t.TaggedItems)
            .Where(t =>
                tagIds.Contains(t.Id)
                && (t.OwnerUserId == null || (viewer.HasValue && t.OwnerUserId == viewer))
            )
            .OrderBy(static t => t.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TagResponse>>([
            .. tags.Select(static t => TagMapper.ToResponse(t)),
        ]);
    }
}
