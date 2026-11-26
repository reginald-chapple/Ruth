using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class GetMetricCategoriesQueryHandler(ReportingDbContext db)
    : IQueryHandler<GetMetricCategoriesQuery, IReadOnlyList<MetricCategoryResponse>>
{
    public async Task<Result<IReadOnlyList<MetricCategoryResponse>>> HandleAsync(
        GetMetricCategoriesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var items = await db
            .MetricCategories.AsNoTracking()
            .OrderBy(static c => c.Order)
            .ThenBy(static c => c.Name)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<MetricCategoryResponse>>([
            .. items.Select(ReportingDtoMapper.ToResponse),
        ]);
    }
}
