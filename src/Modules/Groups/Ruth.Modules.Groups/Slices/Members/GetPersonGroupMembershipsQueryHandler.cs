using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GetPersonGroupMembershipsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetPersonGroupMembershipsQuery, IReadOnlyList<GroupMemberResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberResponse>>> HandleAsync(
        GetPersonGroupMembershipsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var membersQuery = dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => m.PersonId == query.PersonId);
        if (!query.IncludeArchived)
        {
            membersQuery = membersQuery.Where(static m => !m.IsArchived);
        }

        if (query.ActiveOnly)
        {
            membersQuery = membersQuery.Where(static m => m.Status == GroupMemberStatus.Active);
        }

        var members = await membersQuery
            .OrderBy(static m => m.GroupOrder ?? int.MaxValue)
            .ThenBy(static m => m.DateTimeAddedUtc)
            .ToListAsync(cancellationToken);

        var groupIds = members.Select(static m => m.GroupId).Distinct().ToArray();
        var groupNames = await dbContext
            .Groups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(static g => g.Id, static g => g.Name, cancellationToken);

        var roleIds = members.Select(static m => m.GroupRoleId).Distinct().ToArray();
        var roles = await dbContext
            .GroupTypeRoles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(static r => r.Id, cancellationToken);

        return Result.Success<IReadOnlyList<GroupMemberResponse>>([
            .. members.Select(m =>
                GroupMemberMapper.ToResponse(
                    m,
                    groupNames.GetValueOrDefault(m.GroupId, "Group"),
                    roles.GetValueOrDefault(m.GroupRoleId)
                )
            ),
        ]);
    }
}
