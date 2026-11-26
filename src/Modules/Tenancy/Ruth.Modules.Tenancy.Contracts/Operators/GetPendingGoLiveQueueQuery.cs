using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Contracts.Operators;

/// <summary>Queries tenants awaiting go-live approval or filtered by lifecycle stage for the Platform Operator console.</summary>
/// <param name="LifecycleFilter">Optional lifecycle stage filter (<see langword="null" /> returns all tenants).</param>
public sealed record GetPendingGoLiveQueueQuery(TenantLifecycleStage? LifecycleFilter = null)
    : IQuery<IReadOnlyList<TenantSummaryResponse>>,
        IPlatformControlPlaneRequest;
