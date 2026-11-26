using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.MergeTemplates;

internal sealed class GetMergeTemplatesQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetMergeTemplatesQuery, IReadOnlyList<MergeTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<MergeTemplateResponse>>> HandleAsync(
        GetMergeTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.MergeTemplates.AsNoTracking();
        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MergeTemplateResponse>>([
            .. items.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
