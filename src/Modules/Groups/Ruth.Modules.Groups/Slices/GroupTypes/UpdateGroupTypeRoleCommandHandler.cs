using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class UpdateGroupTypeRoleCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpdateGroupTypeRoleCommand, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        UpdateGroupTypeRoleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupTypeId.From(command.GroupTypeId);
        var groupType = await dbContext
            .GroupTypes.Include(static gt => gt.Roles)
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == id, cancellationToken);

        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var updated = groupType.UpdateRole(GroupTypeRoleId.From(command.RoleId), command.Role);
        if (updated.IsFailure)
        {
            return Result.Failure<GroupTypeDetailResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupTypeMapper.ToDetail(groupType));
    }
}
