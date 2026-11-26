namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Specification for group placement configuration on a RegistrationTemplate.</summary>
public sealed record RegistrationGroupPlacementSpec(
    string Name,
    Guid GroupTypeId,
    Guid? DefaultTargetGroupId = null,
    Guid? DefaultGroupRoleId = null,
    bool AutoPlaceOnConfirmation = false
);
