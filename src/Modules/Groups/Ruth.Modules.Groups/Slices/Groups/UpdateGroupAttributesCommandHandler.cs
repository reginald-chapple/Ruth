using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class UpdateGroupAttributesCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpdateGroupAttributesCommand, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        UpdateGroupAttributesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        foreach (var (key, value) in command.Attributes)
        {
            if (value is null)
            {
                group.RemoveAttributeValue(key);
            }
            else
            {
                group.SetAttributeValue(key, value);
            }
        }

        dbContext.Entry(group).Property("Attributes").IsModified = true;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var groupTypeName = await dbContext
            .GroupTypes.AsNoTracking()
            .Where(gt => gt.Id == group.GroupTypeId)
            .Select(static gt => gt.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Group";

        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                group,
                groupTypeName,
                cancellationToken
            )
        );
    }
}
