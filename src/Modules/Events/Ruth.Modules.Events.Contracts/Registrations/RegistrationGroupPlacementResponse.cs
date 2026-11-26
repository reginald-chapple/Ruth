namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for group placement settings on a RegistrationTemplate.</summary>
public sealed record RegistrationGroupPlacementResponse(
    Guid Id,
    string Name,
    Guid GroupTypeId,
    Guid? DefaultTargetGroupId,
    Guid? DefaultGroupRoleId,
    bool AutoPlaceOnConfirmation
);
