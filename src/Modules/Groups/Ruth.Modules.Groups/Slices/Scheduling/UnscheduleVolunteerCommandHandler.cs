using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class UnscheduleVolunteerCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UnscheduleVolunteerCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        UnscheduleVolunteerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var attendanceId = AttendanceId.From(command.AttendanceId);
        var attendance = await dbContext.Attendances.FirstOrDefaultAsync(
            a => a.Id == attendanceId,
            cancellationToken
        );
        if (attendance is null)
        {
            return GroupsErrors.RsvpNotFound;
        }

        _ = dbContext.Attendances.Remove(attendance);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
