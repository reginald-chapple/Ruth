using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Groups;

public sealed partial class Group
{
    internal static Result<Group> Create(
        GroupType groupType,
        GroupInput input,
        DateTimeOffset nowUtc,
        bool isSystem = false
    )
    {
        var group = new Group(GroupId.Create(), groupType.Id, isSystem);
        var apply = group.ApplyInput(groupType, input, nowUtc);
        return apply.IsFailure ? Result.Failure<Group>(apply.Errors) : Result.Success(group);
    }

    internal Result Update(GroupType groupType, GroupInput input, DateTimeOffset nowUtc)
    {
        return ApplyInput(groupType, input, nowUtc);
    }

    internal Result SetArchiveState(bool archive, Guid? archivedByPersonId, DateTimeOffset nowUtc)
    {
        if (archive)
        {
            IsArchived = true;
            IsActive = false;
            ArchivedDateTimeUtc ??= nowUtc.ToUniversalTime();
            InactiveDateTimeUtc ??= nowUtc.ToUniversalTime();
            ArchivedByPersonId = archivedByPersonId;
        }
        else
        {
            IsArchived = false;
            ArchivedDateTimeUtc = null;
            ArchivedByPersonId = null;
        }

        return Result.Success();
    }

    internal Result<GroupLocation> AttachOrUpdateLocation(
        GroupType groupType,
        Guid locationId,
        string? locationName,
        Guid? groupLocationTypeValueId,
        bool isMailingLocation,
        bool isMappedLocation,
        bool isOverflowLocation,
        Guid? groupMemberPersonId,
        int order,
        GroupGeoPointDto? geoPoint,
        IReadOnlyList<GroupGeoPointDto>? geoFenceRing,
        IReadOnlyList<Guid>? scheduleIds
    )
    {
        var existing = _locations.FirstOrDefault(l => l.LocationId == locationId);
        if (existing is not null)
        {
            var updated = existing.Update(
                locationName,
                groupLocationTypeValueId,
                isMailingLocation,
                isMappedLocation,
                isOverflowLocation,
                groupMemberPersonId,
                order,
                geoPoint,
                geoFenceRing,
                scheduleIds
            );
            return updated.IsFailure
                ? Result.Failure<GroupLocation>(updated.Errors)
                : Result.Success(existing);
        }

        if (!groupType.AllowMultipleLocations && _locations.Count >= 1)
        {
            return GroupsErrors.MultipleLocationsNotAllowed;
        }

        var created = GroupLocation.Create(
            Id,
            locationId,
            locationName,
            groupLocationTypeValueId,
            isMailingLocation,
            isMappedLocation,
            isOverflowLocation,
            groupMemberPersonId,
            order,
            geoPoint,
            geoFenceRing,
            scheduleIds
        );
        if (created.IsSuccess)
        {
            _locations.Add(created.Value);
        }

        return created;
    }

    internal Result RemoveLocation(GroupLocationId groupLocationId)
    {
        var existing = _locations.FirstOrDefault(l => l.Id == groupLocationId);
        if (existing is null)
        {
            return GroupsErrors.GroupLocationNotFound;
        }

        _ = _locations.Remove(existing);
        return Result.Success();
    }

    internal void RepointPersonReferences(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ScheduleCoordinatorPersonId == retiredPersonId)
        {
            ScheduleCoordinatorPersonId = survivingPersonId;
        }

        if (GroupAdministratorPersonId == retiredPersonId)
        {
            GroupAdministratorPersonId = survivingPersonId;
        }

        if (ArchivedByPersonId == retiredPersonId)
        {
            ArchivedByPersonId = survivingPersonId;
        }

        foreach (var location in _locations)
        {
            if (location.GroupMemberPersonId == retiredPersonId)
            {
                location.RepointGroupMemberPerson(survivingPersonId);
            }
        }
    }

    private Result ApplyInput(GroupType groupType, GroupInput input, DateTimeOffset nowUtc)
    {
        if (input.ParentGroupId.HasValue && input.ParentGroupId.Value == Id)
        {
            return GroupsErrors.ParentGroupCycle;
        }

        if (groupType.GroupsRequireBranch && !input.BranchId.HasValue)
        {
            return GroupsErrors.BranchRequired;
        }

        if (input.GroupCapacity is < 0)
        {
            return GroupsErrors.InvalidCapacity;
        }

        var nameResult = GroupsTextPolicy.NormalizeRequired(input.Name);
        var descResult = GroupsTextPolicy.NormalizeOptional(input.Description);
        var combined = Result.Combine(nameResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        var wasActive = IsActive;
        Name = nameResult.Value;
        ParentGroupId = input.ParentGroupId;
        BranchId = input.BranchId;
        ScheduleId = input.ScheduleId;
        Description = descResult.Value;
        PhotoId = input.PhotoId;
        MeetingStyle = input.MeetingStyle;
        IsActive = !IsArchived && input.IsActive;
        IsPublic = input.IsPublic;
        Order = input.Order;
        AllowGuests = input.AllowGuests;
        GroupCapacity = input.GroupCapacity;
        StatusValueId = input.StatusValueId;
        RequiredSignatureDocumentTemplateId = input.RequiredSignatureDocumentTemplateId;
        SchedulingMustMeetRequirements = input.SchedulingMustMeetRequirements;
        ScheduleCoordinatorPersonId = input.ScheduleCoordinatorPersonId;
        GroupAdministratorPersonId = input.GroupAdministratorPersonId;
        DisableScheduling = input.DisableScheduling;
        DisableScheduleToolboxAccess = input.DisableScheduleToolboxAccess;
        RsvpReminderOffsetDays = input.RsvpReminderOffsetDays;

        if (!IsActive && wasActive)
        {
            InactiveDateTimeUtc = nowUtc.ToUniversalTime();
        }
        else if (IsActive)
        {
            InactiveDateTimeUtc = null;
        }

        return Result.Success();
    }
}
