using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetRsvpByTokenQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetRsvpByTokenQuery, ScheduledVolunteerResponse>
{
    public async Task<Result<ScheduledVolunteerResponse>> HandleAsync(
        GetRsvpByTokenQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query.RsvpToken))
        {
            return GroupsErrors.RsvpNotFound;
        }

        var token = query.RsvpToken.Trim();
        var attendance = await dbContext
            .Attendances.AsNoTracking()
            .FirstOrDefaultAsync(a => a.RsvpToken == token, cancellationToken);
        if (attendance is null)
        {
            return GroupsErrors.RsvpNotFound;
        }

        var occurrence = await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .FirstAsync(o => o.Id == attendance.OccurrenceId, cancellationToken);

        var groupName = "Group";
        string? locationName = null;
        if (occurrence.GroupId.HasValue)
        {
            var group = await dbContext
                .Groups.AsNoTracking()
                .Include(static g => g.Locations)
                .FirstOrDefaultAsync(g => g.Id == occurrence.GroupId.Value, cancellationToken);
            if (group is not null)
            {
                groupName = group.Name;
                locationName = group
                    .Locations.FirstOrDefault(l => l.LocationId == occurrence.LocationId)
                    ?.LocationName;
            }
        }

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
}
