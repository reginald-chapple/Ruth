using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class RemoveGroupScheduleExclusionCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<RemoveGroupScheduleExclusionCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemoveGroupScheduleExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupScheduleExclusionId.From(command.ExclusionId);
        var existing = await dbContext.GroupScheduleExclusions.FirstOrDefaultAsync(
            e => e.Id == id,
            cancellationToken
        );
        if (existing is null)
        {
            return GroupsErrors.ExclusionNotFound;
        }

        _ = dbContext.GroupScheduleExclusions.Remove(existing);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
