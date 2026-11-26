using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Bookmarks;

internal sealed class GetPersonalLinkSectionsQueryHandler(CmsDbContext db)
    : IQueryHandler<GetPersonalLinkSectionsQuery, IReadOnlyList<PersonalLinkSectionResponse>>
{
    public async Task<Result<IReadOnlyList<PersonalLinkSectionResponse>>> HandleAsync(
        GetPersonalLinkSectionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db
            .PersonalLinkSections.AsNoTracking()
            .Include(static s => s.Links)
            .Where(s =>
                (query.IncludeShared && s.IsShared)
                || (query.PersonId != null && s.PersonId == query.PersonId)
            );

        var list = await q.OrderByDescending(static s => s.IsShared)
            .ThenBy(static s => s.Order)
            .ThenBy(static s => s.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PersonalLinkSectionResponse>>([
            .. list.Select(CmsContentProjectionMapper.ToResponse),
        ]);
    }
}
