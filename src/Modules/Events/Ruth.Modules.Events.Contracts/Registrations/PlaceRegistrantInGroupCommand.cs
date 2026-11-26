using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Command to place a confirmed RegistrationRegistrant into a destination Group via Groups.Contracts.</summary>
public sealed record PlaceRegistrantInGroupCommand(
    Guid RegistrationId,
    Guid RegistrantId,
    Guid TargetGroupId,
    Guid? TargetGroupRoleId = null
) : ICommand<RegistrationRegistrantResponse>;
