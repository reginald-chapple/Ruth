namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Role definition belonging to a GroupType.</summary>
public sealed record GroupTypeRoleResponse(
    Guid Id,
    Guid GroupTypeId,
    string Name,
    string? Description,
    int Order,
    bool IsSystem,
    int? MinCount,
    int? MaxCount,
    bool IsLeader,
    bool ReceiveRequirementsNotifications,
    bool CanView,
    bool CanEdit,
    bool CanManageMembers,
    bool CanTakeAttendance,
    bool IsCheckInAllowed,
    bool IsPublic
);
