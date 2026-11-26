using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class PrintCheckInSessionLabelsCommandHandler(
    CheckInDbContext dbContext,
    SessionLabelPrintCollaborator collaborator
) : ICommandHandler<PrintCheckInSessionLabelsCommand, PrintCheckInSessionLabelsResponse>
{
    public async Task<Result<PrintCheckInSessionLabelsResponse>> HandleAsync(
        PrintCheckInSessionLabelsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = AttendanceCheckInSessionId.From(command.SessionId);
        var session = await dbContext
            .AttendanceCheckInSessions.AsNoTracking()
            .Include(static s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<PrintCheckInSessionLabelsResponse>(CheckInErrors.SessionNotFound);
        }

        var activeLabels = await LoadActiveLabelsForConfigAsync(
            session.CheckInConfigurationId.Value,
            cancellationToken
        );
        var routing = await collaborator.ResolvePrinterRoutingAsync(
            command,
            session,
            cancellationToken
        );

        var jobsResult = await CompileAndDispatchAllJobsAsync(
            session,
            activeLabels,
            routing,
            cancellationToken
        );
        if (jobsResult.IsFailure)
        {
            return Result.Failure<PrintCheckInSessionLabelsResponse>(jobsResult.Errors);
        }

        var serverCount = jobsResult.Value.Count(static j => j.DispatchedFromServer);
        var clientCount = jobsResult.Value.Count - serverCount;
        return Result.Success(
            new PrintCheckInSessionLabelsResponse(
                session.Id.Value,
                serverCount,
                clientCount,
                jobsResult.Value
            )
        );
    }

    private Task<List<CheckInLabel>> LoadActiveLabelsForConfigAsync(
        Guid configId,
        CancellationToken cancellationToken
    )
    {
        return dbContext
            .CheckInLabels.AsNoTracking()
            .Where(l =>
                l.IsActive && (!l.ConfigurationId.HasValue || l.ConfigurationId == configId)
            )
            .OrderBy(static l => l.PrintOrder)
            .ToListAsync(cancellationToken);
    }

    private async Task<Result<List<PrintedLabelJobDto>>> CompileAndDispatchAllJobsAsync(
        AttendanceCheckInSession session,
        List<CheckInLabel> activeLabels,
        (bool PrintFromServer, PrinterEndpointTarget? Endpoint) routing,
        CancellationToken cancellationToken
    )
    {
        var jobs = new List<PrintedLabelJobDto>();

        foreach (var entry in session.Entries.Where(static e => e.PrintBadgeRequested))
        {
            var badgeLabels = SelectLabelsForEntry(activeLabels, entry.CheckInAreaId.Value);
            var ctx = await collaborator.BuildParticipantMergeContextAsync(
                session,
                entry,
                cancellationToken
            );

            foreach (var label in badgeLabels)
            {
                var jobResult = await CompileAndRouteSingleJobAsync(
                    label,
                    entry.Id.Value,
                    ctx,
                    routing,
                    cancellationToken
                );
                if (jobResult.IsFailure)
                {
                    return Result.Failure<List<PrintedLabelJobDto>>(jobResult.Errors);
                }

                jobs.Add(jobResult.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(session.SecurityPickupCode))
        {
            var receiptResult = await AppendGuardianReceiptsAsync(
                session,
                activeLabels,
                routing,
                jobs,
                cancellationToken
            );
            if (receiptResult.IsFailure)
            {
                return Result.Failure<List<PrintedLabelJobDto>>(receiptResult.Errors);
            }
        }

        return Result.Success(jobs);
    }

    private static List<CheckInLabel> SelectLabelsForEntry(
        List<CheckInLabel> activeLabels,
        Guid areaId
    )
    {
        var areaSpecific = activeLabels
            .Where(l =>
                l.LabelKind == CheckInLabelKind.ParticipantBadge
                && l.AreaId.HasValue
                && l.AreaId.Value == areaId
            )
            .ToList();
        return areaSpecific.Count > 0
            ? areaSpecific
            :
            [
                .. activeLabels.Where(static l =>
                    l.LabelKind == CheckInLabelKind.ParticipantBadge && !l.AreaId.HasValue
                ),
            ];
    }

    private async Task<Result> AppendGuardianReceiptsAsync(
        AttendanceCheckInSession session,
        List<CheckInLabel> activeLabels,
        (bool PrintFromServer, PrinterEndpointTarget? Endpoint) routing,
        List<PrintedLabelJobDto> jobs,
        CancellationToken cancellationToken
    )
    {
        var receiptLabels = activeLabels
            .Where(static l => l.LabelKind == CheckInLabelKind.GuardianPickupReceipt)
            .ToList();
        if (receiptLabels.Count == 0)
        {
            return Result.Success();
        }

        var summary = string.Join(
            "; ",
            session.Entries.Select(static e => $"{e.PersonFullName} ({e.LocationName})")
        );
        var inv = CultureInfo.InvariantCulture;
        var ctx = new CheckInLabelMergeContextDto(
            GuardianReceiptSummary: summary,
            SecurityPickupCode: session.SecurityPickupCode,
            ServiceDateText: session.ServiceDate.ToString("yyyy-MM-dd", inv),
            CheckedInTimeText: session.CompletedOnUtc.ToString("HH:mm", inv) + " UTC",
            BarcodeValue: session.SecurityPickupCode
        );

        foreach (var label in receiptLabels)
        {
            var jobResult = await CompileAndRouteSingleJobAsync(
                label,
                entryId: null,
                ctx,
                routing,
                cancellationToken
            );
            if (jobResult.IsFailure)
            {
                return Result.Failure(jobResult.Errors);
            }

            jobs.Add(jobResult.Value);
        }

        return Result.Success();
    }

    private async Task<Result<PrintedLabelJobDto>> CompileAndRouteSingleJobAsync(
        CheckInLabel label,
        Guid? entryId,
        CheckInLabelMergeContextDto ctx,
        (bool PrintFromServer, PrinterEndpointTarget? Endpoint) routing,
        CancellationToken cancellationToken
    )
    {
        var preview = ZplLabelCompiler.Compile(
            label.Name,
            label.Layout,
            ctx,
            label.Copies,
            label.CustomZplOverride
        );

        if (!routing.PrintFromServer || routing.Endpoint is null)
        {
            return Result.Success(
                new PrintedLabelJobDto(
                    label.Id.Value,
                    label.Name,
                    label.LabelKind,
                    entryId,
                    DispatchedFromServer: false,
                    PrinterEndpoint: null,
                    BytesSent: 0,
                    preview.CompiledZpl,
                    preview
                )
            );
        }

        var dispatch = await collaborator.DispatchToServerPrinterAsync(
            routing.Endpoint,
            preview.CompiledZpl,
            cancellationToken
        );
        if (dispatch.IsFailure)
        {
            return Result.Failure<PrintedLabelJobDto>(dispatch.Errors);
        }

        var endpointText = string.Create(
            CultureInfo.InvariantCulture,
            $"{dispatch.Value.ResolvedHost}:{dispatch.Value.ResolvedPort}"
        );
        return Result.Success(
            new PrintedLabelJobDto(
                label.Id.Value,
                label.Name,
                label.LabelKind,
                entryId,
                DispatchedFromServer: true,
                endpointText,
                dispatch.Value.BytesSent,
                preview.CompiledZpl,
                preview
            )
        );
    }
}
