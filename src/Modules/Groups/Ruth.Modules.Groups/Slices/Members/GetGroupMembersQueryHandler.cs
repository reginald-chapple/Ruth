using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GetGroupMembersQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberResponse>>> HandleAsync(
        GetGroupMembersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(query.GroupId);
        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var roles = await dbContext
            .GroupTypeRoles.AsNoTracking()
            .Where(r => r.GroupTypeId == group.GroupTypeId)
            .ToDictionaryAsync(static r => r.Id, cancellationToken);

        var membersQuery = dbContext.GroupMembers.AsNoTracking().Where(m => m.GroupId == groupId);
        if (!query.IncludeArchived)
        {
            membersQuery = membersQuery.Where(static m => !m.IsArchived);
        }

        if (query.Status.HasValue)
        {
            membersQuery = membersQuery.Where(m => m.Status == query.Status.Value);
        }

        var members = await membersQuery
            .OrderBy(static m => m.Status)
            .ThenBy(static m => m.DateTimeAddedUtc)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupMemberResponse>>([
            .. members.Select(m =>
                GroupMemberMapper.ToResponse(m, group.Name, roles.GetValueOrDefault(m.GroupRoleId))
            ),
        ]);
    }
}
