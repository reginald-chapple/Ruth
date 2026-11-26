using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class GetContentChannelTypesQueryHandler(CmsDbContext db)
    : IQueryHandler<GetContentChannelTypesQuery, IReadOnlyList<ContentChannelTypeResponse>>
{
    public async Task<Result<IReadOnlyList<ContentChannelTypeResponse>>> HandleAsync(
        GetContentChannelTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var list = await db
            .ContentChannelTypes.AsNoTracking()
            .OrderBy(static t => t.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ContentChannelTypeResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
