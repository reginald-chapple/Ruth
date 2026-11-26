namespace Ruth.SharedKernel.Tenancy;

/// <summary>
/// ExecutionContext-flowing (<see cref="AsyncLocal{T}" />) tenant context ensuring child DI scopes,
/// EF Core interceptors, caches, Outbox processors, and Blazor Server circuits preserve strict tenant isolation.
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    private static readonly AsyncLocal<TenantExecutionState?> _current = new();
    private static TenantExecutionState? TestHarnessDefault { get; set; }

    /// <summary>Gets the singleton accessor instance.</summary>
    public static AmbientTenantContext Instance { get; } = new();

    /// <summary>Gets the active <see cref="TenantExecutionState" /> (fail-closed <see cref="TenantExecutionState.Unresolved" /> when unset).</summary>
    public static TenantExecutionState CurrentState =>
        _current.Value ?? TestHarnessDefault ?? TenantExecutionState.Unresolved;

    /// <summary>Gets the active <see cref="TenantId" /> or <see cref="TenantId.Empty" />.</summary>
    public static TenantId CurrentTenantId => CurrentState.TenantId;

    /// <inheritdoc />
    public TenantId TenantId => CurrentState.TenantId;

    /// <inheritdoc />
    public string? TenantSlug => CurrentState.TenantSlug;

    /// <inheritdoc />
    public TenantLifecycleStage LifecycleStage => CurrentState.LifecycleStage;

    /// <inheritdoc />
    public bool IsPlatformOperatorHost => CurrentState.IsPlatformOperatorHost;

    /// <inheritdoc />
    public bool HasTenant => !CurrentState.TenantId.IsEmpty;

    /// <inheritdoc />
    public bool IsLive => HasTenant && LifecycleStage == TenantLifecycleStage.Live;

    /// <summary>Sets the tenant execution state for the remainder of the current async flow (e.g., HTTP middleware or Blazor circuit).</summary>
    public static void SetCurrent(TenantExecutionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _current.Value = state;
    }

    /// <summary>Clears any explicit tenant state on the current async flow.</summary>
    public static void ClearCurrent()
    {
        _current.Value = null;
    }

    /// <summary>Enters a lexical tenant execution scope that restores the prior state on disposal.</summary>
    public static TenantScope BeginScope(
        TenantId tenantId,
        string? tenantSlug = null,
        TenantLifecycleStage lifecycleStage = TenantLifecycleStage.Live
    )
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Scoped TenantId cannot be empty.", nameof(tenantId));
        }

        var prior = _current.Value;
        _current.Value = new TenantExecutionState(
            tenantId,
            tenantSlug,
            lifecycleStage,
            IsPlatformOperatorHost: false
        );
        return new TenantScope(prior);
    }

    /// <summary>Enters an explicit unresolved (no-tenant) scope, suppressing any test-harness default to test fail-closed behavior.</summary>
    public static TenantScope BeginUnresolvedScope()
    {
        var prior = _current.Value;
        _current.Value = TenantExecutionState.Unresolved;
        return new TenantScope(prior);
    }

    /// <summary>Enters a lexical platform-operator control-plane scope with no active child tenant.</summary>
    public static TenantScope BeginPlatformOperatorScope()
    {
        var prior = _current.Value;
        _current.Value = new TenantExecutionState(
            TenantId.Empty,
            TenantSlug: null,
            TenantLifecycleStage.Unresolved,
            IsPlatformOperatorHost: true
        );
        return new TenantScope(prior);
    }

    /// <summary>
    /// Configures a process-wide fallback tenant strictly for standalone single-module test harnesses
    /// that do not execute through HTTP host resolution middleware. Never invoke in production hosts.
    /// </summary>
    public static void SetTestHarnessDefault(
        TenantId? tenantId,
        TenantLifecycleStage stage = TenantLifecycleStage.Live
    )
    {
        TestHarnessDefault = tenantId is { IsEmpty: false } id
            ? new TenantExecutionState(id, "test-tenant", stage)
            : null;
    }

    /// <summary>Restores the previous <see cref="TenantExecutionState" /> upon disposal.</summary>
    public readonly struct TenantScope : IDisposable
    {
        private readonly TenantExecutionState? _prior;

        internal TenantScope(TenantExecutionState? prior)
        {
            _prior = prior;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _current.Value = _prior;
        }
    }
}
