using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to fetch a Registration by confirmation code or GUID.</summary>
public sealed record GetRegistrationByConfirmationOrIdQuery(string ConfirmationCodeOrId)
    : IQuery<RegistrationResponse>;
