using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal sealed class GetAttendanceOccurrenceByIdQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetAttendanceOccurrenceByIdQuery, AttendanceOccurrenceDetailResponse>
{
    public async Task<Result<AttendanceOccurrenceDetailResponse>> HandleAsync(
        GetAttendanceOccurrenceByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = AttendanceOccurrenceId.From(query.OccurrenceId);
        var occ = await dbContext
            .AttendanceOccurrences.AsNoTracking()
            .Include(static o => o.Attendances)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (occ is null)
        {
            return GroupsErrors.AttendanceOccurrenceNotFound;
        }

        string? groupName = null;
        if (occ.GroupId.HasValue)
        {
            groupName = await dbContext
                .Groups.AsNoTracking()
                .Where(g => g.Id == occ.GroupId.Value)
                .Select(static g => g.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return Result.Success(AttendanceMapper.ToDetail(occ, groupName));
    }
}
