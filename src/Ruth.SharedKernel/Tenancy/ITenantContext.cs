namespace Ruth.SharedKernel.Tenancy;

/// <summary>Provides read-only access to the current execution scope's isolated tenant identity.</summary>
public interface ITenantContext
{
    /// <summary>Gets the resolved tenant identifier, or <see cref="TenantId.Empty" /> when unresolved.</summary>
    TenantId TenantId { get; }

    /// <summary>Gets the optional normalized tenant slug.</summary>
    string? TenantSlug { get; }

    /// <summary>Gets the operational compliance lifecycle stage of the resolved tenant.</summary>
    TenantLifecycleStage LifecycleStage { get; }

    /// <summary>Gets whether the current request targets the dedicated platform operator console host.</summary>
    bool IsPlatformOperatorHost { get; }

    /// <summary>Gets whether a non-empty tenant is bound to the current execution context.</summary>
    bool HasTenant { get; }

    /// <summary>Gets whether the bound tenant is IRS-verified and approved for live operations.</summary>
    bool IsLive { get; }
}
