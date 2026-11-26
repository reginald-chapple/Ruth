using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Reports;

internal sealed class GetReportsQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetReportsQuery, IReadOnlyList<ReportResponse>>
{
    public async Task<Result<IReadOnlyList<ReportResponse>>> HandleAsync(
        GetReportsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = db.Reports.AsNoTracking().Include(static r => r.Fields).AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static r => r.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            var key = query.EntityTypeKey.Trim();
            q = q.Where(r => r.EntityTypeKey == key);
        }

        var list = await q.OrderBy(static r => r.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ReportResponse>>([
            .. list.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
