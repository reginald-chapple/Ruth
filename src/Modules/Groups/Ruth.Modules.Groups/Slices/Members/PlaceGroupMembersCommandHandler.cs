using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class PlaceGroupMembersCommandHandler(
    GroupsDbContext dbContext,
    GroupMemberAddService addService,
    IClock clock
) : ICommandHandler<PlaceGroupMembersCommand, GroupPlacementResultResponse>
{
    public async Task<Result<GroupPlacementResultResponse>> HandleAsync(
        PlaceGroupMembersCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var outcomes = new List<GroupPlacementOutcomeItem>(command.Assignments.Count);
        var placed = 0;
        var failed = 0;
        var sourceGroupId = command.RemoveFromSourceGroupId.HasValue
            ? GroupId.From(command.RemoveFromSourceGroupId.Value)
            : (GroupId?)null;

        foreach (var assignment in command.Assignments)
        {
            var outcome = await PlaceSingleAssignmentAsync(
                assignment,
                sourceGroupId,
                cancellationToken
            );
            outcomes.Add(outcome);
            if (outcome.Succeeded)
            {
                placed++;
            }
            else
            {
                failed++;
            }
        }

        return Result.Success(new GroupPlacementResultResponse(placed, failed, outcomes));
    }

    private async Task<GroupPlacementOutcomeItem> PlaceSingleAssignmentAsync(
        GroupPlacementAssignmentDto assignment,
        GroupId? sourceGroupId,
        CancellationToken cancellationToken
    )
    {
        var result = await addService.AddMemberAsync(
            new AddGroupMemberCommand(
                assignment.DestinationGroupId,
                assignment.PersonId,
                assignment.DestinationRoleId,
                assignment.Status
            ),
            cancellationToken
        );

        if (result.IsFailure)
        {
            return new GroupPlacementOutcomeItem(
                assignment.PersonId,
                assignment.DestinationGroupId,
                Succeeded: false,
                GroupMemberId: null,
                Warning: null,
                result.PrimaryError.Description
            );
        }

        if (sourceGroupId.HasValue && sourceGroupId.Value.Value != assignment.DestinationGroupId)
        {
            await ArchiveSourceMembershipAsync(
                sourceGroupId.Value,
                assignment.PersonId,
                cancellationToken
            );
        }

        return new GroupPlacementOutcomeItem(
            assignment.PersonId,
            assignment.DestinationGroupId,
            Succeeded: true,
            result.Value.Id,
            result.Value.CapacityWarning,
            ErrorMessage: null
        );
    }

    private async Task ArchiveSourceMembershipAsync(
        GroupId sourceGroupId,
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var sourceMembers = await dbContext
            .GroupMembers.Where(m =>
                m.GroupId == sourceGroupId && m.PersonId == personId && !m.IsArchived
            )
            .ToListAsync(cancellationToken);
        if (sourceMembers.Count == 0)
        {
            return;
        }

        var nowUtc = clock.UtcNow;
        foreach (var member in sourceMembers)
        {
            member.Archive(archivedByPersonId: null, nowUtc);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
