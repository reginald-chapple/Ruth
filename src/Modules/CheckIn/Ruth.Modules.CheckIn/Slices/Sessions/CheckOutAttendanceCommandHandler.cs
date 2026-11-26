using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class CheckOutAttendanceCommandHandler(CheckInDbContext dbContext, IClock clock)
    : ICommandHandler<CheckOutAttendanceCommand, CheckOutResultResponse>
{
    public async Task<Result<CheckOutResultResponse>> HandleAsync(
        CheckOutAttendanceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var configId = CheckInConfigurationId.From(command.ConfigurationId);
        var config = await dbContext
            .CheckInConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<CheckOutResultResponse>(CheckInErrors.ConfigurationNotFound);
        }

        if (!config.AllowCheckout)
        {
            return Result.Failure<CheckOutResultResponse>(CheckInErrors.CheckOutNotAllowed);
        }

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var serviceDate = command.ServiceDate ?? DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var targetEntryIds = await ResolveTargetEntryIdsAsync(
            configId,
            command,
            serviceDate,
            cancellationToken
        );
        if (targetEntryIds.Count == 0)
        {
            return Result.Failure<CheckOutResultResponse>(CheckInErrors.EntryNotFound);
        }

        var sessions = await dbContext
            .AttendanceCheckInSessions.Include(static s => s.Entries)
            .Where(s => s.Entries.Any(e => targetEntryIds.Contains(e.Id)))
            .ToListAsync(cancellationToken);

        var updatedResult = ApplyCheckOuts(sessions, targetEntryIds, command, nowUtc);
        if (updatedResult.IsFailure)
        {
            return Result.Failure<CheckOutResultResponse>(updatedResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            new CheckOutResultResponse(updatedResult.Value.Count, nowUtc, updatedResult.Value)
        );
    }

    private static Result<List<CheckedInEntryReceiptDto>> ApplyCheckOuts(
        List<Domain.Sessions.AttendanceCheckInSession> sessions,
        List<CheckInAttendanceEntryId> targetEntryIds,
        CheckOutAttendanceCommand command,
        DateTimeOffset nowUtc
    )
    {
        var updatedDtos = new List<CheckedInEntryReceiptDto>(targetEntryIds.Count);
        foreach (var session in sessions)
        {
            foreach (var entryId in targetEntryIds)
            {
                if (!session.Entries.Any(e => e.Id == entryId))
                {
                    continue;
                }

                var outcome = session.CheckOutEntry(
                    entryId,
                    nowUtc,
                    command.CheckedOutByPersonId,
                    command.SecurityPickupCode,
                    command.IsStaffOverride,
                    command.OverrideReason
                );
                if (outcome.IsFailure)
                {
                    return Result.Failure<List<CheckedInEntryReceiptDto>>(outcome.Errors);
                }

                updatedDtos.Add(CheckInSessionMapper.ToEntryReceipt(outcome.Value));
            }
        }

        return Result.Success(updatedDtos);
    }

    private async Task<List<CheckInAttendanceEntryId>> ResolveTargetEntryIdsAsync(
        CheckInConfigurationId configId,
        CheckOutAttendanceCommand command,
        DateOnly serviceDate,
        CancellationToken cancellationToken
    )
    {
        if (command.EntryIds is { Count: > 0 })
        {
            return
            [
                .. command
                    .EntryIds.Where(static id => id != Guid.Empty)
                    .Distinct()
                    .Select(CheckInAttendanceEntryId.From),
            ];
        }

        if (string.IsNullOrWhiteSpace(command.SecurityPickupCode))
        {
            return [];
        }

        var normalizedCode = command.SecurityPickupCode.Trim().ToUpperInvariant();
        return await dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e =>
                e.CheckInConfigurationId == configId
                && e.ServiceDate == serviceDate
                && e.Status == CheckInEntryStatus.CheckedIn
                && e.SecurityPickupCode == normalizedCode
            )
            .Select(static e => e.Id)
            .ToListAsync(cancellationToken);
    }
}
