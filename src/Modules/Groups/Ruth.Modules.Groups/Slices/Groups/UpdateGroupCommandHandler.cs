using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class UpdateGroupCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<UpdateGroupCommand, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        UpdateGroupCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var parentId = command.ParentGroupId.HasValue
            ? GroupId.From(command.ParentGroupId.Value)
            : (GroupId?)null;
        var parentCheck = await GroupHistoryHelper.ValidateParentAssignmentAsync(
            dbContext,
            group.Id,
            group.GroupTypeId,
            parentId,
            cancellationToken
        );
        if (parentCheck.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(parentCheck.Errors);
        }

        var nowUtc = clock.UtcNow;
        var updated = group.Update(groupType, BuildInput(command, parentId), nowUtc);
        if (updated.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(updated.Errors);
        }

        await GroupHistoryHelper.RotateSnapshotIfEnabledAsync(
            dbContext,
            group,
            groupType,
            nowUtc,
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                group,
                groupType.Name,
                cancellationToken
            )
        );
    }

    private static GroupInput BuildInput(UpdateGroupCommand command, GroupId? parentId)
    {
        return new GroupInput(
            command.Name,
            parentId,
            command.CampusId,
            command.ScheduleId,
            command.Description,
            command.PhotoId,
            command.MeetingStyle,
            command.IsActive,
            command.IsPublic,
            command.Order,
            command.AllowGuests,
            command.GroupCapacity,
            command.StatusValueId,
            command.RequiredSignatureDocumentTemplateId,
            command.SchedulingMustMeetRequirements,
            command.ScheduleCoordinatorPersonId,
            command.GroupAdministratorPersonId,
            command.DisableScheduling,
            command.DisableScheduleToolboxAccess,
            command.RsvpReminderOffsetDays
        );
    }
}
