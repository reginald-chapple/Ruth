using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain.Events;

/// <summary>Domain event raised when a Platform Operator approves a verified tax-exempt tenant for Live operations.</summary>
public sealed record TenantApprovedLiveDomainEvent(
    Guid EventId,
    TenantId TenantId,
    string OperatorEmail,
    bool ManualIrsOverrideApproved,
    DateTimeOffset OccurredOnUtc
) : IDomainEvent;
