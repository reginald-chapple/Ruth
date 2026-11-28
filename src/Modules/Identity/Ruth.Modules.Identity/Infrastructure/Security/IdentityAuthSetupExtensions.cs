using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ruth.Modules.Identity.Domain;

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
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                StealthHostCookieManager.ConfigureCookieOptions
            )
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                IdentityAuthorizationPolicies.ApiKeyScheme,
                static _ => { }
            );

        _ = services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<IdentityJwtOptions>>(
                static (options, jwtOptions) => ConfigureJwtBearerOptions(options, jwtOptions.Value)
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
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidAlgorithms = [JwtBearerSecurityEvents.PinnedAlgorithm],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "unique_name",
            RoleClaimType = ClaimTypes.Role,
        };
        options.Events = JwtBearerSecurityEvents.Create();
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
