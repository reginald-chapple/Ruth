using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal sealed class GetGroupAttendanceOccurrencesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<
        GetGroupAttendanceOccurrencesQuery,
        IReadOnlyList<AttendanceOccurrenceSummaryResponse>
    >
{
    public async Task<Result<IReadOnlyList<AttendanceOccurrenceSummaryResponse>>> HandleAsync(
        GetGroupAttendanceOccurrencesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(query.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var occQuery = dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Include(static o => o.Attendances)
            .Where(o => o.GroupId == groupId);

        if (query.StartDate.HasValue)
        {
            occQuery = occQuery.Where(o => o.OccurrenceDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            occQuery = occQuery.Where(o => o.OccurrenceDate <= query.EndDate.Value);
        }

        var max = Math.Clamp(query.MaxResults, 1, 250);
        var rows = await occQuery
            .OrderByDescending(static o => o.OccurrenceDate)
            .Take(max)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AttendanceOccurrenceSummaryResponse>>([
            .. rows.Select(o => AttendanceMapper.ToSummary(o, group.Name)),
        ]);
    }
}
