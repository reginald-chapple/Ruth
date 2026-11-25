using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class GetGroupMemberRequirementsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupMemberRequirementsQuery, IReadOnlyList<GroupMemberRequirementResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberRequirementResponse>>> HandleAsync(
        GetGroupMemberRequirementsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var memberId = GroupMemberId.From(query.GroupMemberId);
        var member = await dbContext
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);
        if (member is null)
        {
            return GroupsErrors.MemberNotFound;
        }

        var rows = await dbContext
            .GroupMemberRequirements.AsNoTracking()
            .Where(mr => mr.GroupMemberId == memberId)
            .ToListAsync(cancellationToken);
        var reqs = await dbContext
            .GroupRequirements.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, cancellationToken);
        var types = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        var responses = new List<GroupMemberRequirementResponse>(rows.Count);
        foreach (var row in rows)
        {
            if (
                reqs.TryGetValue(row.GroupRequirementId, out var req)
                && types.TryGetValue(req.GroupRequirementTypeId, out var type)
            )
            {
                responses.Add(
                    GroupRequirementEvaluatorService.ToMemberRequirementResponse(row, member, type)
                );
            }
        }

        return Result.Success<IReadOnlyList<GroupMemberRequirementResponse>>(responses);
    }
}
