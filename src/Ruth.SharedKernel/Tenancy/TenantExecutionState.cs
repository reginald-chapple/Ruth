namespace Ruth.SharedKernel.Tenancy;

/// <summary>Immutable snapshot of the active tenant execution boundary.</summary>
public sealed record TenantExecutionState(
    TenantId TenantId,
    string? TenantSlug = null,
    TenantLifecycleStage LifecycleStage = TenantLifecycleStage.Live,
    bool IsPlatformOperatorHost = false
)
{
    /// <summary>Empty unresolved state (fail-closed).</summary>
    public static TenantExecutionState Unresolved { get; } =
        new(TenantId.Empty, TenantSlug: null, TenantLifecycleStage.Unresolved);
}
