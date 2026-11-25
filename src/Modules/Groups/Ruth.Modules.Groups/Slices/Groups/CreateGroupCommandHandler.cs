using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class CreateGroupCommandHandler(GroupsDbContext dbContext, IClock clock)
    : ICommandHandler<CreateGroupCommand, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        CreateGroupCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupTypeId = GroupTypeId.From(command.GroupTypeId);
        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == groupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var parentId = command.ParentGroupId.HasValue
            ? GroupId.From(command.ParentGroupId.Value)
            : (GroupId?)null;
        var parentCheck = await GroupHistoryHelper.ValidateParentAssignmentAsync(
            dbContext,
            currentGroupId: null,
            groupType.Id,
            parentId,
            cancellationToken
        );
        if (parentCheck.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(parentCheck.Errors);
        }

        var nowUtc = clock.UtcNow;
        var created = Group.Create(groupType, BuildInput(command, parentId), nowUtc);
        if (created.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(created.Errors);
        }

        ApplyInitialAttributes(created.Value, command.Attributes);
        _ = dbContext.Groups.Add(created.Value);
        await GroupHistoryHelper.RotateSnapshotIfEnabledAsync(
            dbContext,
            created.Value,
            groupType,
            nowUtc,
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                created.Value,
                groupType.Name,
                cancellationToken
            )
        );
    }

    private static GroupInput BuildInput(CreateGroupCommand command, GroupId? parentId)
    {
        return new GroupInput(
            command.Name,
            parentId,
            command.BranchId,
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

    private static void ApplyInitialAttributes(
        Group group,
        IReadOnlyDictionary<string, string?>? attributes
    )
    {
        if (attributes is null)
        {
            return;
        }

        foreach (var (key, value) in attributes)
        {
            group.SetAttributeValue(key, value);
        }
    }
}
