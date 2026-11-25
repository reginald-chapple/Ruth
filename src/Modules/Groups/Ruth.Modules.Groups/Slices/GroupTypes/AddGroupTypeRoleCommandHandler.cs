using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class AddGroupTypeRoleCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<AddGroupTypeRoleCommand, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        AddGroupTypeRoleCommand command,
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

        var added = groupType.AddRole(command.Role, command.SetAsDefault);
        if (added.IsFailure)
        {
            return Result.Failure<GroupTypeDetailResponse>(added.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupTypeMapper.ToDetail(groupType));
    }
}
