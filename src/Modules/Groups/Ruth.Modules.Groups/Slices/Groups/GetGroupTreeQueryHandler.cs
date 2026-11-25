using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class GetGroupTreeQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupTreeQuery, IReadOnlyList<GroupTreeNodeResponse>>
{
    public async Task<Result<IReadOnlyList<GroupTreeNodeResponse>>> HandleAsync(
        GetGroupTreeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var typeLookup = await dbContext
            .GroupTypes.AsNoTracking()
            .ToDictionaryAsync(
                static gt => gt.Id,
                static gt => (gt.Name, gt.ShowInNavigation),
                cancellationToken
            );

        var groupsQuery = dbContext.Groups.AsNoTracking().AsQueryable();
        if (query.GroupTypeId.HasValue)
        {
            var typeId = GroupTypeId.From(query.GroupTypeId.Value);
            groupsQuery = groupsQuery.Where(g => g.GroupTypeId == typeId);
        }

        if (query.BranchId.HasValue)
        {
            groupsQuery = groupsQuery.Where(g => g.BranchId == query.BranchId.Value);
        }

        if (!query.IncludeInactive)
        {
            groupsQuery = groupsQuery.Where(static g => g.IsActive);
        }

        if (!query.IncludeArchived)
        {
            groupsQuery = groupsQuery.Where(static g => !g.IsArchived);
        }

        var groups = await groupsQuery
            .OrderBy(static g => g.Order)
            .ThenBy(static g => g.Name)
            .ToListAsync(cancellationToken);

        if (query.NavigationOnly)
        {
            groups =
            [
                .. groups.Where(g =>
                    typeLookup.TryGetValue(g.GroupTypeId, out var info) && info.ShowInNavigation
                ),
            ];
        }

        var activeCounts = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(static m => !m.IsArchived && m.Status == GroupMemberStatus.Active)
            .GroupBy(static m => m.GroupId)
            .Select(static g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.GroupId, static x => x.Count, cancellationToken);

        return Result.Success(BuildTree(groups, typeLookup, activeCounts));
    }

    private static IReadOnlyList<GroupTreeNodeResponse> BuildTree(
        List<Group> groups,
        Dictionary<GroupTypeId, (string Name, bool ShowInNavigation)> typeLookup,
        Dictionary<GroupId, int> activeCounts
    )
    {
        var presentIds = new HashSet<GroupId>(groups.Select(static g => g.Id));
        var childrenByParent = groups
            .Where(g => g.ParentGroupId.HasValue && presentIds.Contains(g.ParentGroupId.Value))
            .GroupBy(static g => g.ParentGroupId!.Value)
            .ToDictionary(static grp => grp.Key, static grp => grp.ToList());

        var roots = groups.Where(g =>
            !g.ParentGroupId.HasValue || !presentIds.Contains(g.ParentGroupId.Value)
        );

        return
        [
            .. roots.Select(root =>
                BuildNode(root, childrenByParent, typeLookup, activeCounts, visited: [])
            ),
        ];
    }

    private static GroupTreeNodeResponse BuildNode(
        Group group,
        Dictionary<GroupId, List<Group>> childrenByParent,
        Dictionary<GroupTypeId, (string Name, bool ShowInNavigation)> typeLookup,
        Dictionary<GroupId, int> activeCounts,
        HashSet<GroupId> visited
    )
    {
        _ = visited.Add(group.Id);
        GroupTreeNodeResponse[] children = [];
        if (childrenByParent.TryGetValue(group.Id, out var childGroups))
        {
            children =
            [
                .. childGroups
                    .Where(c => !visited.Contains(c.Id))
                    .Select(c => BuildNode(c, childrenByParent, typeLookup, activeCounts, visited)),
            ];
        }

        var typeName = typeLookup.TryGetValue(group.GroupTypeId, out var info)
            ? info.Name
            : "Group";

        return new GroupTreeNodeResponse(
            group.Id.Value,
            group.GroupTypeId.Value,
            typeName,
            group.ParentGroupId?.Value,
            group.BranchId,
            group.Name,
            group.IsActive,
            group.IsPublic,
            group.IsArchived,
            group.Order,
            activeCounts.GetValueOrDefault(group.Id, 0),
            group.GroupCapacity,
            children
        );
    }
}
