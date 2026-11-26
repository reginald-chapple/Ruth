using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to list configured RegistrationTemplates.</summary>
public sealed record GetRegistrationTemplatesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<RegistrationTemplateResponse>>;
