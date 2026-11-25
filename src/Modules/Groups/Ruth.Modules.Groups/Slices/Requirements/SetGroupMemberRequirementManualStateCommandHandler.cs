using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class SetGroupMemberRequirementManualStateCommandHandler(
    GroupsDbContext dbContext,
    IClock clock
) : ICommandHandler<SetGroupMemberRequirementManualStateCommand, GroupMemberRequirementResponse>
{
    public async Task<Result<GroupMemberRequirementResponse>> HandleAsync(
        SetGroupMemberRequirementManualStateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var memberId = GroupMemberId.From(command.GroupMemberId);
        var reqId = GroupRequirementId.From(command.GroupRequirementId);

        var member = await dbContext
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId, cancellationToken);
        if (member is null)
        {
            return GroupsErrors.MemberNotFound;
        }

        var requirement = await dbContext
            .GroupRequirements.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reqId, cancellationToken);
        if (requirement is null)
        {
            return GroupsErrors.RequirementNotFound;
        }

        var type = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == requirement.GroupRequirementTypeId,
                cancellationToken
            );
        if (type is null)
        {
            return GroupsErrors.RequirementTypeNotFound;
        }

        var row = await dbContext.GroupMemberRequirements.FirstOrDefaultAsync(
            mr => mr.GroupMemberId == memberId && mr.GroupRequirementId == reqId,
            cancellationToken
        );
        if (row is null)
        {
            row = GroupMemberRequirement.Create(memberId, reqId, dueDate: null);
            _ = dbContext.GroupMemberRequirements.Add(row);
        }

        var updated = row.SetManualOrOverride(
            command.IsOverride,
            requirement.AllowLeadersToOverride,
            command.IsMet,
            command.ActorPersonId,
            command.Note,
            clock.UtcNow
        );
        if (updated.IsFailure)
        {
            return Result.Failure<GroupMemberRequirementResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            GroupRequirementEvaluatorService.ToMemberRequirementResponse(row, member, type)
        );
    }
}
