using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class RespondToRsvpCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<RespondToRsvpCommand, ScheduledVolunteerResponse>
{
    public async Task<Result<ScheduledVolunteerResponse>> HandleAsync(
        RespondToRsvpCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var attendance = await FindAttendanceAsync(command, cancellationToken);
        if (attendance is null)
        {
            return GroupsErrors.RsvpNotFound;
        }

        var apply = attendance.ApplyRsvp(
            command.Response,
            command.DeclineReasonValueId,
            command.DeclineReasonNote,
            clock.UtcNow
        );
        if (apply.IsFailure)
        {
            return Result.Failure<ScheduledVolunteerResponse>(apply.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var occurrence = await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .FirstAsync(o => o.Id == attendance.OccurrenceId, cancellationToken);

        var (groupName, locationName) = await ResolveNamesAsync(occurrence, cancellationToken);
        return Result.Success(
            ScheduleVolunteerService.ToScheduledResponse(
                attendance,
                occurrence,
                groupName,
                locationName,
                hasBlackout: false,
                hasDoubleBooking: false,
                meetsRequirements: true
            )
        );
    }

    private async Task<Attendance?> FindAttendanceAsync(
        RespondToRsvpCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.AttendanceId.HasValue)
        {
            var id = AttendanceId.From(command.AttendanceId.Value);
            return await dbContext.Attendances.FirstOrDefaultAsync(
                a => a.Id == id,
                cancellationToken
            );
        }

        if (string.IsNullOrWhiteSpace(command.RsvpToken))
        {
            return null;
        }

        var token = command.RsvpToken.Trim();
        return await dbContext.Attendances.FirstOrDefaultAsync(
            a => a.RsvpToken == token,
            cancellationToken
        );
    }

    private async Task<(string GroupName, string? LocationName)> ResolveNamesAsync(
        AttendanceOccurrence occurrence,
        CancellationToken cancellationToken
    )
    {
        if (!occurrence.GroupId.HasValue)
        {
            return ("Group", null);
        }

        var group = await dbContext
            .Groups.AsNoTracking()
            .Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == occurrence.GroupId.Value, cancellationToken);
        if (group is null)
        {
            return ("Group", null);
        }

        var locName = group
            .Locations.FirstOrDefault(l => l.LocationId == occurrence.LocationId)
            ?.LocationName;
        return (group.Name, locName);
    }
}
