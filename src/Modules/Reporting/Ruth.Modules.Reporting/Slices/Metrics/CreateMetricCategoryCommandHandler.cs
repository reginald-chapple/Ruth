using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Metrics;

internal sealed class CreateMetricCategoryCommandHandler(ReportingDbContext db)
    : ICommandHandler<CreateMetricCategoryCommand, MetricCategoryResponse>
{
    public async Task<Result<MetricCategoryResponse>> HandleAsync(
        CreateMetricCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        MetricCategoryId? parentId =
            command.ParentCategoryId is { } pid && pid != Guid.Empty
                ? MetricCategoryId.From(pid)
                : null;
        var created = MetricCategory.Create(
            command.Name,
            command.Description,
            parentId,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<MetricCategoryResponse>(created.PrimaryError);
        }

        _ = db.MetricCategories.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(created.Value));
    }
}
