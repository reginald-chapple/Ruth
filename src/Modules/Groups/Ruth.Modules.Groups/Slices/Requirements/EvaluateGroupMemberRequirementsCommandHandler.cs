using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class EvaluateGroupMemberRequirementsCommandHandler(
    GroupsDbContext dbContext,
    GroupRequirementEvaluatorService evaluator
)
    : ICommandHandler<
        EvaluateGroupMemberRequirementsCommand,
        IReadOnlyList<GroupMemberRequirementResponse>
    >
{
    public async Task<Result<IReadOnlyList<GroupMemberRequirementResponse>>> HandleAsync(
        EvaluateGroupMemberRequirementsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var memberId = GroupMemberId.From(command.GroupMemberId);
        var member = await dbContext
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);
        if (member is null)
        {
            return GroupsErrors.MemberNotFound;
        }

        var group = await dbContext
            .Groups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == member.GroupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var result = await evaluator.EvaluateAndPersistMemberAsync(
            member,
            group,
            cancellationToken
        );
        return Result.Success(result);
    }
}
