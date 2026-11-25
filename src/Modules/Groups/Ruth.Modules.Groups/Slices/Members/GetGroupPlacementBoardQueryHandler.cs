using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GetGroupPlacementBoardQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupPlacementBoardQuery, GroupPlacementBoardResponse>
{
    public async Task<Result<GroupPlacementBoardResponse>> HandleAsync(
        GetGroupPlacementBoardQuery query,
        CancellationToken cancellationToken = default
    )
    {
        GroupId[] destIds = [.. query.DestinationGroupIds.Distinct().Select(GroupId.From)];
        var allGroupIds = destIds;
        if (query.SourceGroupId.HasValue)
        {
            allGroupIds = [.. destIds.Append(GroupId.From(query.SourceGroupId.Value)).Distinct()];
        }

        var groups = await dbContext
            .Groups.AsNoTracking()
            .Where(g => allGroupIds.Contains(g.Id))
            .ToDictionaryAsync(static g => g.Id, cancellationToken);
        var members = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => allGroupIds.Contains(m.GroupId) && !m.IsArchived)
            .ToListAsync(cancellationToken);
        var roles = await dbContext
            .GroupTypeRoles.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, cancellationToken);

        var sourceMembers = ProjectSourceMembers(query.SourceGroupId, groups, members, roles);
        var columns = BuildDestinationColumns(destIds, groups, members, roles);
        return Result.Success(
            new GroupPlacementBoardResponse(query.SourceGroupId, sourceMembers, columns)
        );
    }

    private static GroupMemberResponse[] ProjectSourceMembers(
        Guid? sourceGroupId,
        Dictionary<GroupId, Group> groups,
        List<GroupMember> members,
        Dictionary<GroupTypeRoleId, GroupTypeRole> roles
    )
    {
        if (!sourceGroupId.HasValue)
        {
            return [];
        }

        var srcId = GroupId.From(sourceGroupId.Value);
        var srcName = groups.GetValueOrDefault(srcId)?.Name ?? "Source Group";
        return
        [
            .. members
                .Where(m => m.GroupId == srcId)
                .Select(m =>
                    GroupMemberMapper.ToResponse(m, srcName, roles.GetValueOrDefault(m.GroupRoleId))
                ),
        ];
    }

    private static List<GroupPlacementDestinationColumnResponse> BuildDestinationColumns(
        GroupId[] destIds,
        Dictionary<GroupId, Group> groups,
        List<GroupMember> members,
        Dictionary<GroupTypeRoleId, GroupTypeRole> roles
    )
    {
        var columns = new List<GroupPlacementDestinationColumnResponse>(destIds.Length);
        foreach (var destId in destIds)
        {
            if (!groups.TryGetValue(destId, out var destGroup))
            {
                continue;
            }

            GroupMemberResponse[] destMembers =
            [
                .. members
                    .Where(m => m.GroupId == destId)
                    .Select(m =>
                        GroupMemberMapper.ToResponse(
                            m,
                            destGroup.Name,
                            roles.GetValueOrDefault(m.GroupRoleId)
                        )
                    ),
            ];
            columns.Add(
                new GroupPlacementDestinationColumnResponse(
                    destGroup.Id.Value,
                    destGroup.Name,
                    destGroup.GroupCapacity,
                    destMembers.Count(static m => m.Status == GroupMemberStatus.Active),
                    destMembers.Count(static m => m.Status == GroupMemberStatus.Pending),
                    destMembers
                )
            );
        }

        return columns;
    }
}
