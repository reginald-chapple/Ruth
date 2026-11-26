using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class GetCmsBlockTypesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetCmsBlockTypesQuery, IReadOnlyList<CmsBlockTypeResponse>>
{
    public async Task<Result<IReadOnlyList<CmsBlockTypeResponse>>> HandleAsync(
        GetCmsBlockTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var list = await db
            .BlockTypes.AsNoTracking()
            .OrderBy(static b => b.Category)
            .ThenBy(static b => b.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CmsBlockTypeResponse>>([
            .. list.Select(CmsProjectionMapper.ToResponse),
        ]);
    }
}
