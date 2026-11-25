using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Attaches or updates a physical/geographic location on a Group.</summary>
public sealed record AttachGroupLocationCommand(
    Guid GroupId,
    Guid LocationId,
    string? LocationName = null,
    Guid? GroupLocationTypeValueId = null,
    bool IsMailingLocation = false,
    bool IsMappedLocation = true,
    bool IsOverflowLocation = false,
    Guid? GroupMemberPersonId = null,
    int Order = 0,
    GroupGeoPointDto? GeoPoint = null,
    IReadOnlyList<GroupGeoPointDto>? GeoFenceRing = null,
    IReadOnlyList<Guid>? ScheduleIds = null
) : ICommand<GroupDetailResponse>;
