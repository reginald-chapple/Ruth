using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal static class IdentityAuthSetupExtensions
{
    public static IServiceCollection AddIdentityAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = services
            .AddOptions<IdentityJwtOptions>()
            .Bind(configuration.GetSection(IdentityJwtOptions.SectionName));

        var jwtOptions = new IdentityJwtOptions();
        configuration.GetSection(IdentityJwtOptions.SectionName).Bind(jwtOptions);

        _ = services
            .AddAuthentication(static options =>
            {
                options.DefaultScheme = IdentityAuthorizationPolicies.SmartScheme;
                options.DefaultAuthenticateScheme = IdentityAuthorizationPolicies.SmartScheme;
                options.DefaultChallengeScheme = IdentityAuthorizationPolicies.SmartScheme;
            })
            .AddPolicyScheme(
                IdentityAuthorizationPolicies.SmartScheme,
                "JWT Bearer, REST API Key, or Cookie",
                static options => options.ForwardDefaultSelector = SelectAuthenticationScheme
            )
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, ConfigureCookieOptions)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                ConfigureJwtBearerOptions(options, jwtOptions)
            )
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                IdentityAuthorizationPolicies.ApiKeyScheme,
                static _ => { }
            );

        _ = services.AddAuthorization(static options =>
        {
            options.AddPolicy(IdentityAuthorizationPolicies.AdministratorsOnly, static policy =>
                policy.RequireAuthenticatedUser().RequireRole(SecurityRole.AdministratorsRoleName)
            );
            options.AddPolicy("PlatformOperatorPolicy", static policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(RuthClaimTypes.PlatformOperator, "true")
            );
        });

        _ = services.AddScoped<IAuthorizationHandler, EntityAuthorizationHandler>();
        return services;
    }

    private static void ConfigureCookieOptions(CookieAuthenticationOptions options)
    {
        // Stealth white-label host-only session cookie name (zero framework/platform fingerprinting).
        options.Cookie.Name = "sid";
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

    private static async Task ValidateCookieTenantBindingAsync(
        CookieValidatePrincipalContext context
    )
    {
        var state = AmbientTenantContext.CurrentState;
        if (state.IsPlatformOperatorHost)
        {
            var isOp = context.Principal?.HasClaim(RuthClaimTypes.PlatformOperator, "true") == true;
            if (!isOp)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme
                );
            }

            return;
        }

        if (state.TenantId.IsEmpty)
        {
            return;
        }

        var tokenTid = context.Principal?.FindFirst(RuthClaimTypes.TenantId)?.Value;
        if (!Guid.TryParse(tokenTid, out var parsed) || parsed != state.TenantId.Value)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );
        }
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

    private static void ConfigureJwtBearerOptions(
        JwtBearerOptions options,
        IdentityJwtOptions jwtOptions
    )
    {
        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.ResolveEffectiveSigningKey())
        );

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "unique_name",
            RoleClaimType = ClaimTypes.Role,
        };
        options.Events = new JwtBearerEvents { OnTokenValidated = ValidateJwtTenantBindingAsync };
    }

    private static Task ValidateJwtTenantBindingAsync(TokenValidatedContext context)
    {
        var activeTenantId = AmbientTenantContext.CurrentTenantId;
        if (activeTenantId.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var tokenTid = context.Principal?.FindFirst(RuthClaimTypes.TenantId)?.Value;
        if (!Guid.TryParse(tokenTid, out var parsed) || parsed != activeTenantId.Value)
        {
            context.Fail("Access token tenant claim does not match active workspace.");
        }

        return Task.CompletedTask;
    }

    private static string SelectAuthenticationScheme(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        return authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
            : context.Request.Headers.ContainsKey(
                ApiKeyAuthenticationHandler.AuthorizationTokenHeader
            ) || context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.ApiKeyHeader)
                ? IdentityAuthorizationPolicies.ApiKeyScheme
            : CookieAuthenticationDefaults.AuthenticationScheme;
    }
}
