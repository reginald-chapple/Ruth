using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Query to list RegistrationInstances with live seat and waitlist counts.</summary>
public sealed record GetRegistrationInstancesQuery(
    Guid? TemplateId = null,
    Guid? BranchId = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<RegistrationInstanceResponse>>;
