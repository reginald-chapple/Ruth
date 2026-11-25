using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetGroupMemberAssignmentsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupMemberAssignmentsQuery, IReadOnlyList<GroupMemberAssignmentResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberAssignmentResponse>>> HandleAsync(
        GetGroupMemberAssignmentsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(query.GroupId);
        var assignments = await dbContext
            .GroupMemberAssignments.AsNoTracking()
            .Where(a => a.GroupId == groupId)
            .ToListAsync(cancellationToken);
        GroupMemberId[] memberIds = [.. assignments.Select(static a => a.GroupMemberId).Distinct()];
        var personByMember = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(static m => m.Id, static m => m.PersonId, cancellationToken);

        return Result.Success<IReadOnlyList<GroupMemberAssignmentResponse>>([
            .. assignments.Select(a =>
                UpsertGroupMemberAssignmentCommandHandler.ToResponse(
                    a,
                    personByMember.GetValueOrDefault(a.GroupMemberId, Guid.Empty)
                )
            ),
        ]);
    }
}
