using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain.Events;

/// <summary>Domain event raised when a Sandbox tenant passes initial readiness checks and submits for Go-Live review.</summary>
public sealed record TenantSubmittedForGoLiveDomainEvent(
    Guid EventId,
    TenantId TenantId,
    IrsVerificationStatus IrsStatus,
    string StripeConnectAccountId,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;
