using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class CreateGroupTypeCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<CreateGroupTypeCommand, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        CreateGroupTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
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
            command.GroupsRequireCampus,
            command.EnableGroupHistory,
            command.TakesAttendance,
            command.AttendanceCountsAsWeekendService,
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

        var created = GroupType.Create(input, command.InitialRoles, childIds);
        if (created.IsFailure)
        {
            return Result.Failure<GroupTypeDetailResponse>(created.Errors);
        }

        _ = dbContext.GroupTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(GroupTypeMapper.ToDetail(created.Value));
    }
}
