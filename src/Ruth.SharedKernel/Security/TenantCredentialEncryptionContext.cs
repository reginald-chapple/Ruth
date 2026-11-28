namespace Ruth.SharedKernel.Security;

/// <summary>
/// Ambient and process-wide accessor for <see cref="ITenantCredentialProtector" /> used by EF Core value converters
/// while supporting isolated <see cref="AsyncLocal{T}" /> key-ring scopes in tests.
/// </summary>
public static class TenantCredentialEncryptionContext
{
    private static readonly AsyncLocal<ITenantCredentialProtector?> _scopedOverride = new();
    private static volatile ITenantCredentialProtector _defaultProtector =
        new AesGcmTenantCredentialProtector();

    /// <summary>Gets the active <see cref="ITenantCredentialProtector" /> for the current execution flow.</summary>
    public static ITenantCredentialProtector Current => _scopedOverride.Value ?? _defaultProtector;

    /// <summary>Sets the process-wide default <see cref="ITenantCredentialProtector" />.</summary>
    public static void ConfigureDefault(ITenantCredentialProtector protector)
    {
        ArgumentNullException.ThrowIfNull(protector);
        _defaultProtector = protector;
    }

    /// <summary>Pushes an execution-flow-scoped <see cref="ITenantCredentialProtector" /> override.</summary>
    public static IDisposable PushScoped(ITenantCredentialProtector protector)
    {
        ArgumentNullException.ThrowIfNull(protector);
        var previous = _scopedOverride.Value;
        _scopedOverride.Value = protector;
        return new ScopeRestorer(previous);
    }

    private sealed class ScopeRestorer(ITenantCredentialProtector? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scopedOverride.Value = previous;
        }
    }
}
