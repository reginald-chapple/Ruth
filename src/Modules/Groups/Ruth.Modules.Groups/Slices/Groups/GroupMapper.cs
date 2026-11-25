using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;

namespace Ruth.Modules.Groups.Slices.Groups;

internal static class GroupMapper
{
    public static async Task<GroupDetailResponse> LoadDetailResponseAsync(
        GroupsDbContext dbContext,
        Group group,
        string groupTypeName,
        CancellationToken cancellationToken
    )
    {
        var activeCount = await dbContext.GroupMembers.CountAsync(
            m => m.GroupId == group.Id && !m.IsArchived && m.Status == GroupMemberStatus.Active,
            cancellationToken
        );
        var pendingCount = await dbContext.GroupMembers.CountAsync(
            m => m.GroupId == group.Id && !m.IsArchived && m.Status == GroupMemberStatus.Pending,
            cancellationToken
        );

        var demographicValues = await dbContext
            .GroupDemographicValues.AsNoTracking()
            .Where(v => v.GroupId == group.Id)
            .ToListAsync(cancellationToken);

        var demographicTypeNames = await dbContext
            .GroupDemographicTypes.AsNoTracking()
            .Where(t => t.GroupTypeId == group.GroupTypeId)
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        GroupLocationResponse[] locations =
        [
            .. group
                .Locations.OrderBy(static l => l.Order)
                .ThenBy(static l => l.LocationName, StringComparer.OrdinalIgnoreCase)
                .Select(ToLocationResponse),
        ];

        GroupDemographicValueResponse[] demographics =
        [
            .. demographicValues.Select(v =>
                ToDemographicValueResponse(
                    v,
                    demographicTypeNames.GetValueOrDefault(v.GroupDemographicTypeId, "Demographic")
                )
            ),
        ];

        return BuildDetailResponse(
            group,
            groupTypeName,
            activeCount,
            pendingCount,
            locations,
            demographics
        );
    }

    private static GroupDetailResponse BuildDetailResponse(
        Group group,
        string groupTypeName,
        int activeCount,
        int pendingCount,
        GroupLocationResponse[] locations,
        GroupDemographicValueResponse[] demographics
    )
    {
        return new GroupDetailResponse(
            group.Id.Value,
            group.GroupTypeId.Value,
            groupTypeName,
            group.ParentGroupId?.Value,
            group.CampusId,
            group.ScheduleId,
            group.Name,
            group.Description,
            group.PhotoId,
            group.MeetingStyle,
            group.IsActive,
            group.IsPublic,
            group.IsArchived,
            group.IsSystem,
            group.Order,
            group.AllowGuests,
            group.GroupCapacity,
            activeCount,
            pendingCount,
            group.StatusValueId,
            group.RequiredSignatureDocumentTemplateId,
            group.InactiveDateTimeUtc,
            group.ArchivedDateTimeUtc,
            group.ArchivedByPersonId,
            group.SchedulingMustMeetRequirements,
            group.ScheduleCoordinatorPersonId,
            group.GroupAdministratorPersonId,
            group.DisableScheduling,
            group.DisableScheduleToolboxAccess,
            group.RsvpReminderOffsetDays,
            locations,
            demographics,
            new Dictionary<string, string?>(
                group.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            group.CreatedOnUtc,
            group.ModifiedOnUtc
        );
    }

    public static GroupLocationResponse ToLocationResponse(GroupLocation location)
    {
        return new GroupLocationResponse(
            location.Id.Value,
            location.GroupId.Value,
            location.LocationId,
            location.LocationName,
            location.GroupLocationTypeValueId,
            location.IsMailingLocation,
            location.IsMappedLocation,
            location.IsOverflowLocation,
            location.GroupMemberPersonId,
            location.Order,
            GroupsGeoHelper.ToPointDto(location.GeoPoint),
            GroupsGeoHelper.ToRingDto(location.GeoFence),
            [.. location.ScheduleIds]
        );
    }

    public static GroupDemographicTypeResponse ToDemographicTypeResponse(GroupDemographicType type)
    {
        return new GroupDemographicTypeResponse(
            type.Id.Value,
            type.GroupTypeId.Value,
            type.Name,
            type.Description,
            type.ComponentKey,
            type.IsAutomated,
            type.RunOnPersonUpdate
        );
    }

    public static GroupDemographicValueResponse ToDemographicValueResponse(
        GroupDemographicValue value,
        string demographicTypeName
    )
    {
        return new GroupDemographicValueResponse(
            value.Id.Value,
            value.GroupId.Value,
            value.GroupDemographicTypeId.Value,
            demographicTypeName,
            value.Value,
            value.ValueAsNumeric,
            value.ValueAsBoolean,
            value.ValueAsGuid,
            value.RelatedEntityTypeKey,
            value.RelatedEntityId,
            value.LastCalculatedOnUtc
        );
    }

    public static GroupHistoricalResponse ToHistoricalResponse(GroupHistorical row)
    {
        return new GroupHistoricalResponse(
            row.Id.Value,
            row.GroupId.Value,
            row.GroupName,
            row.GroupTypeId.Value,
            row.GroupTypeName,
            row.CampusId,
            row.ParentGroupId?.Value,
            row.Description,
            row.StatusValueId,
            row.ScheduleId,
            row.IsActive,
            row.IsArchived,
            row.InactiveDateTimeUtc,
            row.ArchivedDateTimeUtc,
            row.ArchivedByPersonId,
            row.EffectiveDateTimeUtc,
            row.ExpireDateTimeUtc,
            row.CurrentRowIndicator
        );
    }
}
