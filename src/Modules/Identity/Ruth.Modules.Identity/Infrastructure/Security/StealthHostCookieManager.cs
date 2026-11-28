using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Enforces RFC 6265bis <c lang="http">__Host-</c> session cookie semantics (<c lang="http">__Host-sid</c> with
/// <c lang="http">Secure; Path=/; HttpOnly</c> and no <c lang="http">Domain</c>) on HTTPS requests while preserving
/// host-only <c lang="http">sid</c> cookies on plain HTTP development/test harness requests.
/// </summary>
public sealed class StealthHostCookieManager : ICookieManager
{
    /// <summary>Neutral host-only session cookie name used on plain HTTP development/test requests.</summary>
    public const string PlainHostCookieName = "sid";

    /// <summary>RFC 6265bis <c lang="http">__Host-</c> session cookie name enforced on HTTPS requests.</summary>
    public const string SecureHostPrefixedCookieName = "__Host-sid";

    private readonly ChunkingCookieManager _inner = new();

    /// <inheritdoc />
    public string? GetRequestCookie(HttpContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        var effectiveKey = ResolveCookieName(context, key);
        return _inner.GetRequestCookie(context, effectiveKey);
    }

    /// <inheritdoc />
    public void AppendResponseCookie(
        HttpContext context,
        string key,
        string? value,
        CookieOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);

        var effectiveKey = ResolveCookieName(context, key);
        EnforceHostOnlyOptions(context, options);
        _inner.AppendResponseCookie(context, effectiveKey, value, options);
    }

    /// <inheritdoc />
    public void DeleteCookie(HttpContext context, string key, CookieOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(options);

        var effectiveKey = ResolveCookieName(context, key);
        EnforceHostOnlyOptions(context, options);
        _inner.DeleteCookie(context, effectiveKey, options);
    }

    internal static void ConfigureCookieOptions(CookieAuthenticationOptions options)
    {
        options.CookieManager = new StealthHostCookieManager();
        options.Cookie.Name = PlainHostCookieName;
        options.Cookie.Domain = null;
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/cookie-logout";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Events.OnValidatePrincipal = ValidateCookieTenantBindingAsync;
        options.Events.OnRedirectToLogin = static context =>
            HandleCookieRedirect(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = static context =>
            HandleCookieRedirect(context, StatusCodes.Status403Forbidden);
    }

    internal static string ResolveCookieName(HttpContext context, string key)
    {
        return
            string.Equals(key, PlainHostCookieName, StringComparison.Ordinal)
            || string.Equals(key, SecureHostPrefixedCookieName, StringComparison.Ordinal)
            ? (context.Request.IsHttps ? SecureHostPrefixedCookieName : PlainHostCookieName)
            : key;
    }

    private static async Task ValidateCookieTenantBindingAsync(
        CookieValidatePrincipalContext context
    )
    {
        var state = AmbientTenantContext.CurrentState;
        var isOp = context.Principal?.HasClaim(RuthClaimTypes.PlatformOperator, "true") == true;
        if (state.IsPlatformOperatorHost)
        {
            if (!isOp)
            {
                await RejectAndSignOutAsync(context);
            }

            return;
        }

        if (isOp)
        {
            await RejectAndSignOutAsync(context);
            return;
        }

        if (state.TenantId.IsEmpty)
        {
            return;
        }

        var tokenTid = context.Principal?.FindFirst(RuthClaimTypes.TenantId)?.Value;
        if (!Guid.TryParse(tokenTid, out var parsed) || parsed != state.TenantId.Value)
        {
            await RejectAndSignOutAsync(context);
        }
    }

    private static Task RejectAndSignOutAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        return context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static Task HandleCookieRedirect(
        RedirectContext<CookieAuthenticationOptions> context,
        int apiStatusCode
    )
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = apiStatusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static void EnforceHostOnlyOptions(HttpContext context, CookieOptions options)
    {
        options.Domain = null;
        options.Path = "/";
        options.HttpOnly = true;
        if (context.Request.IsHttps)
        {
            options.Secure = true;
        }
    }
}
