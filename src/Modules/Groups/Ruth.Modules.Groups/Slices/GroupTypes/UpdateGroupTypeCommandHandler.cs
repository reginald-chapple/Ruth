using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class UpdateGroupTypeCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpdateGroupTypeCommand, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        UpdateGroupTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupTypeId.From(command.GroupTypeId);
        var groupType = await dbContext
            .GroupTypes.Include(static gt => gt.Roles)
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == id, cancellationToken);

        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var input = new GroupTypeInput(
            command.Name,
            command.Description,
            command.GroupTerm,
            command.GroupMemberTerm,
            command.IconCssClass,
            command.Order,
            command.ShowInGroupList,
            command.ShowInNavigation,
            command.AllowMultipleLocations,
            command.IsMeetingStyleEnabled,
            command.GroupsRequireBranch,
            command.EnableGroupHistory,
            command.TakesAttendance,
            command.CountsTowardHeadlineAttendance,
            command.SendAttendanceReminder,
            command.GroupAttendanceRequiresLocation,
            command.GroupAttendanceRequiresSchedule,
            command.AttendanceRule,
            command.GroupCapacityRule,
            command.LocationSelectionMode,
            command.EnableLocationSchedules,
            command.IsSchedulingEnabled,
            command.ScheduleConfirmationLogic,
            command.ScheduleReminderEmailOffsetDays,
            command.PurposeValueId,
            command.InheritedGroupTypeId.HasValue
                ? GroupTypeId.From(command.InheritedGroupTypeId.Value)
                : null
        );

        GroupTypeId[]? childIds = command.AllowedChildGroupTypeIds is null
            ? null
            : [.. command.AllowedChildGroupTypeIds.Select(GroupTypeId.From)];

        var defaultRoleId = command.DefaultGroupRoleId.HasValue
            ? GroupTypeRoleId.From(command.DefaultGroupRoleId.Value)
            : (GroupTypeRoleId?)null;

        var updated = groupType.Update(input, defaultRoleId, childIds);
        if (updated.IsFailure)
        {
            return Result.Failure<GroupTypeDetailResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupTypeMapper.ToDetail(groupType));
    }
}
