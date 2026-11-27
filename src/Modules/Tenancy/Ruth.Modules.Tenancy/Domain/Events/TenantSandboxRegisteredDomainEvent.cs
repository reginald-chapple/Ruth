using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain.Events;

/// <summary>Domain event raised when a new Sandbox organization tenant is registered.</summary>
public sealed record TenantSandboxRegisteredDomainEvent(
    Guid EventId,
    TenantId TenantId,
    string Slug,
    string Ein,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;
