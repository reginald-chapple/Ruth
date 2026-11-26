using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to list Registrations for a specific RegistrationInstance.</summary>
public sealed record GetRegistrationsByInstanceQuery(Guid InstanceId)
    : IQuery<IReadOnlyList<RegistrationResponse>>;
