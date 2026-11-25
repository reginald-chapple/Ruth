namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Input specification for creating or updating a GroupTypeRole.</summary>
public sealed record GroupTypeRoleInputDto(
    string Name,
    string? Description = null,
    int Order = 0,
    int? MinCount = null,
    int? MaxCount = null,
    bool IsLeader = false,
    bool ReceiveRequirementsNotifications = false,
    bool CanView = true,
    bool CanEdit = false,
    bool CanManageMembers = false,
    bool CanTakeAttendance = false,
    bool IsCheckInAllowed = true,
    bool IsPublic = true
);
