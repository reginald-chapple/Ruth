using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class SimpleRegisterForGroupCommandHandler(
    GroupsDbContext dbContext,
    GroupMemberAddService addService
) : ICommandHandler<SimpleRegisterForGroupCommand, GroupMemberResponse>
{
    public async Task<Result<GroupMemberResponse>> HandleAsync(
        SimpleRegisterForGroupCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        if (!group.IsActive || group.IsArchived || !group.IsPublic)
        {
            return GroupsErrors.GroupNotOpenForRegistration;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        if (command.GroupRoleId.HasValue)
        {
            var requestedRole = groupType.Roles.FirstOrDefault(r =>
                r.Id.Value == command.GroupRoleId.Value
            );
            if (requestedRole is null || !requestedRole.IsPublic)
            {
                return GroupsErrors.RoleMismatch;
            }
        }

        return await addService.AddMemberAsync(
            new AddGroupMemberCommand(
                command.GroupId,
                command.PersonId,
                command.GroupRoleId,
                command.InitialStatus,
                command.Note,
                command.GuestCount
            ),
            cancellationToken
        );
    }
}
