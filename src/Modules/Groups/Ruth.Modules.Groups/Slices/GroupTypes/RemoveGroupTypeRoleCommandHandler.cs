using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class RemoveGroupTypeRoleCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<RemoveGroupTypeRoleCommand, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        RemoveGroupTypeRoleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupTypeId.From(command.GroupTypeId);
        var roleId = GroupTypeRoleId.From(command.RoleId);

        var groupType = await dbContext
            .GroupTypes.Include(static gt => gt.Roles)
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == id, cancellationToken);

        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var inUse = await dbContext.GroupMembers.AnyAsync(
            m => m.GroupRoleId == roleId && !m.IsArchived,
            cancellationToken
        );
        if (inUse)
        {
            return GroupsErrors.RoleInUse;
        }

        var removed = groupType.RemoveRole(roleId);
        if (removed.IsFailure)
        {
            return Result.Failure<GroupTypeDetailResponse>(removed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupTypeMapper.ToDetail(groupType));
    }
}
