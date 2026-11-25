using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Tags;

internal sealed class GetTagsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetTagsQuery, IReadOnlyList<TagResponse>>
{
    public async Task<Result<IReadOnlyList<TagResponse>>> HandleAsync(
        GetTagsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .Tags.AsNoTracking()
            .Include(static t => t.TaggedItems)
            .AsQueryable();
        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(t => t.EntityTypeId == null || t.EntityTypeId == etId);
        }

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static t => t.IsActive);
        }

        var viewer = query.ViewerUserId;
        baseQuery = baseQuery.Where(t =>
            t.OwnerUserId == null || (viewer.HasValue && t.OwnerUserId == viewer)
        );

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(t => t.NormalizedName.Contains(term));
        }

        var tags = await baseQuery
            .OrderBy(static t => t.Order)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TagResponse>>([
            .. tags.Select(static t => TagMapper.ToResponse(t)),
        ]);
    }
}
