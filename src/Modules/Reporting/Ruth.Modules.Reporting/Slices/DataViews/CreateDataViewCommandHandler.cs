using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.DataViews;

internal sealed class CreateDataViewCommandHandler(ReportingDbContext db)
    : ICommandHandler<CreateDataViewCommand, DataViewResponse>
{
    public async Task<Result<DataViewResponse>> HandleAsync(
        CreateDataViewCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = DataView.Create(
            command.Name,
            command.EntityTypeKey,
            command.RootFilter,
            command.Description,
            command.CategoryId,
            command.TransformKey,
            command.PersistedScheduleIntervalMinutes
        );
        if (created.IsFailure)
        {
            return Result.Failure<DataViewResponse>(created.PrimaryError);
        }

        _ = db.DataViews.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(created.Value));
    }
}
