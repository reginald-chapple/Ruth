using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain.Events;

/// <summary>Domain event raised when a Platform Operator returns a tenant to Sandbox compliance state.</summary>
public sealed record TenantReturnedToSandboxDomainEvent(
    Guid EventId,
    TenantId TenantId,
    string OperatorEmail,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;
