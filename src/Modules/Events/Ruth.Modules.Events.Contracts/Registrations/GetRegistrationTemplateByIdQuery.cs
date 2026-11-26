using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to fetch a single RegistrationTemplate by identifier.</summary>
public sealed record GetRegistrationTemplateByIdQuery(Guid TemplateId)
    : IQuery<RegistrationTemplateResponse>;
