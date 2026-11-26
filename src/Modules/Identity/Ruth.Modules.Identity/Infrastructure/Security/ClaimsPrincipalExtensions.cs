using System.Security.Claims;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>Helpers converting between <see cref="ClaimsPrincipal" /> and Ruth security primitives.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Extracts the authenticated user account identifier from <paramref name="principal" />, if present.</summary>
    public static Guid? GetUserAccountId(this ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var raw = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    /// <summary>Converts a <see cref="ClaimsPrincipal" /> into an immutable <see cref="SecuritySubject" />.</summary>
    public static SecuritySubject ToSecuritySubject(this ClaimsPrincipal? principal)
    {
        var userId = principal.GetUserAccountId();
        if (!userId.HasValue || principal is null)
        {
            return SecuritySubject.Anonymous;
        }

        var roleIds = new List<Guid>();
        foreach (var claim in principal.FindAll(RuthClaimTypes.RoleId))
        {
            if (Guid.TryParse(claim.Value, out var roleId) && roleId != Guid.Empty)
            {
                roleIds.Add(roleId);
            }
        }

        return new SecuritySubject(userId, roleIds);
    }

    /// <summary>Creates a <see cref="ClaimsPrincipal" /> suitable for ASP.NET Core Cookie sign-in.</summary>
    public static ClaimsPrincipal CreateCookiePrincipal(
        AuthTokensResponse tokens,
        string authenticationScheme
    )
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tokens.UserAccountId.ToString("D")),
            new("sub", tokens.UserAccountId.ToString("D")),
            new(ClaimTypes.Name, tokens.UserName),
            new(ClaimTypes.Email, tokens.Email),
            new(RuthClaimTypes.DisplayName, tokens.DisplayName),
        };

        if (!AmbientTenantContext.CurrentTenantId.IsEmpty)
        {
            claims.Add(
                new Claim(RuthClaimTypes.TenantId, AmbientTenantContext.CurrentTenantId.ToString())
            );
        }

        foreach (var role in tokens.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var roleId in tokens.RoleIds)
        {
            claims.Add(new Claim(RuthClaimTypes.RoleId, roleId.ToString("D")));
        }

        var identity = new ClaimsIdentity(claims, authenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
