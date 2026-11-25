using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class RemoveGroupMemberAssignmentCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<RemoveGroupMemberAssignmentCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemoveGroupMemberAssignmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupMemberAssignmentId.From(command.AssignmentId);
        var existing = await dbContext.GroupMemberAssignments.FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken
        );
        if (existing is null)
        {
            return GroupsErrors.AssignmentNotFound;
        }

        _ = dbContext.GroupMemberAssignments.Remove(existing);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
