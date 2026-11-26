using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.Reports;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Reports;

internal sealed class CreateReportCommandHandler(
    ReportingDbContext db,
    IEnumerable<IFieldSelectPlugin> fieldPlugins
) : ICommandHandler<CreateReportCommand, ReportResponse>
{
    public async Task<Result<ReportResponse>> HandleAsync(
        CreateReportCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var pluginKeys = fieldPlugins
            .Select(static p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var field in command.Fields)
        {
            if (!pluginKeys.Contains(field.FieldSelectKey))
            {
                return Result.Failure<ReportResponse>(
                    ReportingErrors.Validation(
                        "Report.UnknownFieldPlugin",
                        $"Unknown IFieldSelectPlugin '{field.FieldSelectKey}'."
                    )
                );
            }
        }

        DataViewId? dvId = null;
        if (command.DataViewId is { } rawDvId && rawDvId != Guid.Empty)
        {
            dvId = DataViewId.From(rawDvId);
            var exists = await db
                .DataViews.AsNoTracking()
                .AnyAsync(d => d.Id == dvId.Value, cancellationToken);
            if (!exists)
            {
                return Result.Failure<ReportResponse>(
                    ReportingErrors.NotFound("DataView.NotFound", "Backing DataView was not found.")
                );
            }
        }

        var created = Report.Create(
            command.Name,
            command.EntityTypeKey,
            command.Fields,
            dvId,
            command.Description,
            command.CategoryId,
            command.FetchTop
        );
        if (created.IsFailure)
        {
            return Result.Failure<ReportResponse>(created.PrimaryError);
        }

        _ = db.Reports.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(created.Value));
    }
}
