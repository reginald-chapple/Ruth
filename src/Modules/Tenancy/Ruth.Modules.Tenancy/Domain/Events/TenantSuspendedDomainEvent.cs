using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain.Events;

/// <summary>Domain event raised when a Platform Operator suspends a tenant workspace.</summary>
public sealed record TenantSuspendedDomainEvent(
    Guid EventId,
    TenantId TenantId,
    string OperatorEmail,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;
