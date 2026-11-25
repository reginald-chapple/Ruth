using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal static class GroupHistoryHelper
{
    public static async Task RotateSnapshotIfEnabledAsync(
        GroupsDbContext dbContext,
        Group group,
        GroupType groupType,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (!groupType.EnableGroupHistory)
        {
            return;
        }

        var currentRows = await dbContext
            .GroupHistoricals.Where(h => h.GroupId == group.Id && h.CurrentRowIndicator)
            .ToListAsync(cancellationToken);

        foreach (var row in currentRows)
        {
            row.ExpireAt(nowUtc);
        }

        var snapshot = GroupHistorical.CreateSnapshot(group, groupType.Name, nowUtc);
        _ = dbContext.GroupHistoricals.Add(snapshot);
    }

    public static async Task<Result> ValidateParentAssignmentAsync(
        GroupsDbContext dbContext,
        GroupId? currentGroupId,
        GroupTypeId childGroupTypeId,
        GroupId? parentGroupId,
        CancellationToken cancellationToken
    )
    {
        if (!parentGroupId.HasValue)
        {
            return Result.Success();
        }

        if (currentGroupId.HasValue && parentGroupId.Value == currentGroupId.Value)
        {
            return GroupsErrors.ParentGroupCycle;
        }

        var parent = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == parentGroupId.Value, cancellationToken);
        if (parent is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var parentGroupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == parent.GroupTypeId, cancellationToken);
        if (parentGroupType is not null && !parentGroupType.AllowsChildGroupType(childGroupTypeId))
        {
            return GroupsErrors.ParentGroupChildTypeNotAllowed;
        }

        if (!currentGroupId.HasValue)
        {
            return Result.Success();
        }

        var cursor = parent.ParentGroupId;
        var guard = 0;
        while (cursor.HasValue && guard < 128)
        {
            if (cursor.Value == currentGroupId.Value)
            {
                return GroupsErrors.ParentGroupCycle;
            }

            cursor = await dbContext
                .Groups.AsNoTracking()
                .Where(g => g.Id == cursor.Value)
                .Select(static g => g.ParentGroupId)
                .FirstOrDefaultAsync(cancellationToken);
            guard++;
        }

        return Result.Success();
    }
}
