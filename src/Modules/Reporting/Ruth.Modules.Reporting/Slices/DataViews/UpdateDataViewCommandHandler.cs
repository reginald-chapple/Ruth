using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.DataViews;

internal sealed class UpdateDataViewCommandHandler(ReportingDbContext db)
    : ICommandHandler<UpdateDataViewCommand, DataViewResponse>
{
    public async Task<Result<DataViewResponse>> HandleAsync(
        UpdateDataViewCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = DataViewId.From(command.DataViewId);
        var existing = await db
            .DataViews.Include(static d => d.Filters)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure<DataViewResponse>(
                ReportingErrors.NotFound("DataView.NotFound", "DataView was not found.")
            );
        }

        var oldFilters = existing.Filters.ToList();
        var updated = existing.Update(
            command.Name,
            command.RootFilter,
            command.Description,
            command.CategoryId,
            command.TransformKey,
            command.PersistedScheduleIntervalMinutes,
            command.IsActive
        );
        if (updated.IsFailure)
        {
            return Result.Failure<DataViewResponse>(updated.PrimaryError);
        }

        db.DataViewFilters.RemoveRange(oldFilters);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(existing));
    }
}
