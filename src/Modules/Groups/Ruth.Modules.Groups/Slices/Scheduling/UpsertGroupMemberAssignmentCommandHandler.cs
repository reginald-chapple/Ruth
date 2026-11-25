using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class UpsertGroupMemberAssignmentCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpsertGroupMemberAssignmentCommand, GroupMemberAssignmentResponse>
{
    public async Task<Result<GroupMemberAssignmentResponse>> HandleAsync(
        UpsertGroupMemberAssignmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var memberId = GroupMemberId.From(command.GroupMemberId);
        var member = await dbContext
            .GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId && !m.IsArchived, cancellationToken);
        if (member is null)
        {
            return GroupsErrors.MemberNotFound;
        }

        var existing = await dbContext.GroupMemberAssignments.FirstOrDefaultAsync(
            a =>
                a.GroupMemberId == memberId
                && a.LocationId == command.LocationId
                && a.ScheduleId == command.ScheduleId,
            cancellationToken
        );

        if (existing is null)
        {
            existing = GroupMemberAssignment.Create(
                member.GroupId,
                memberId,
                command.LocationId,
                command.ScheduleId
            );
            _ = dbContext.GroupMemberAssignments.Add(existing);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(ToResponse(existing, member.PersonId));
    }

    internal static GroupMemberAssignmentResponse ToResponse(
        GroupMemberAssignment assignment,
        Guid personId
    )
    {
        return new GroupMemberAssignmentResponse(
            assignment.Id.Value,
            assignment.GroupId.Value,
            assignment.GroupMemberId.Value,
            personId,
            assignment.LocationId,
            assignment.ScheduleId,
            assignment.ConfirmationSentOnUtc,
            assignment.LastReminderSentOnUtc
        );
    }
}
