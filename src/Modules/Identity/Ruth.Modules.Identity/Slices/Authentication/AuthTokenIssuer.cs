using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal static class AuthTokenIssuer
{
    public static async Task<AuthTokensResponse> IssueTokensAsync(
        IdentityDbContext dbContext,
        ITokenService tokenService,
        UserAccount account,
        DateTimeOffset utcNow,
        Guid familyId,
        string? ipAddress,
        CancellationToken cancellationToken
    )
    {
        var roleIds = account.Roles.Select(static r => r.SecurityRoleId).ToList();
        var roles =
            roleIds.Count == 0
                ? []
                : await dbContext
                    .SecurityRoles.AsNoTracking()
                    .Where(role => roleIds.Contains(role.Id) && role.IsActive)
                    .OrderBy(static role => role.Name)
                    .ToListAsync(cancellationToken);

        var (accessToken, accessExpires) = tokenService.CreateAccessToken(account, roles, utcNow);
        var rawRefreshToken = tokenService.GenerateOpaqueToken(32);
        var refreshExpires = utcNow.ToUniversalTime().Add(tokenService.RefreshTokenLifetime);
        var refreshEntity = RefreshToken.Create(
            account.Id,
            tokenService.ComputeSha256Hash(rawRefreshToken),
            familyId,
            utcNow,
            refreshExpires,
            ipAddress
        );
        _ = dbContext.RefreshTokens.Add(refreshEntity);

        return new AuthTokensResponse(
            accessToken,
            accessExpires,
            rawRefreshToken,
            refreshExpires,
            account.Id.Value,
            account.UserName,
            account.Email,
            account.DisplayName,
            [.. roles.Select(static r => r.Name)],
            [.. roles.Select(static r => r.Id.Value)],
            account.IsPasswordChangeRequired
        );
    }

    public static async Task<IReadOnlyList<SecurityRole>> LoadActiveRolesAsync(
        IdentityDbContext dbContext,
        UserAccount account,
        CancellationToken cancellationToken
    )
    {
        var roleIds = account.Roles.Select(static r => r.SecurityRoleId).ToList();
        return roleIds.Count == 0
            ? []
            : await dbContext
                .SecurityRoles.AsNoTracking()
                .Where(role => roleIds.Contains(role.Id) && role.IsActive)
                .OrderBy(static role => role.Name)
                .ToListAsync(cancellationToken);
    }
}
