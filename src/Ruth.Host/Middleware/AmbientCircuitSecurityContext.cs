using System.Security.Claims;

namespace Ruth.Host.Middleware;

/// <summary>
/// Propagates the authenticated circuit or SignalR hub <see cref="ClaimsPrincipal" /> across
/// asynchronous continuations and child DI scopes where <see cref="IHttpContextAccessor.HttpContext" /> is null.
/// </summary>
internal static class AmbientCircuitSecurityContext
{
    private static readonly AsyncLocal<ClaimsPrincipal?> _currentPrincipal = new();

    public static ClaimsPrincipal? CurrentPrincipal => _currentPrincipal.Value;

    public static Scope BeginScope(ClaimsPrincipal? principal)
    {
        var prior = _currentPrincipal.Value;
        _currentPrincipal.Value = principal;
        return new Scope(prior);
    }

    public readonly struct Scope(ClaimsPrincipal? prior) : IDisposable
    {
        public void Dispose()
        {
            _currentPrincipal.Value = prior;
        }
    }
}
