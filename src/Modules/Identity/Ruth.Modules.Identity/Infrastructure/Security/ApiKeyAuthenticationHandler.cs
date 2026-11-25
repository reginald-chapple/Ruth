using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthorizationTokenHeader = "Authorization-Token";
    public const string ApiKeyHeader = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawKey = ExtractApiKeyHeader();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return AuthenticateResult.NoResult();
        }

        var utcNow = clock.UtcNow;
        var hash = tokenService.ComputeSha256Hash(rawKey);
        var keyEntity = await dbContext.RestApiKeys.FirstOrDefaultAsync(
            k => k.KeyHash == hash,
            Context.RequestAborted
        );

        if (keyEntity is null || !keyEntity.IsValidAt(utcNow))
        {
            return AuthenticateResult.Fail("Invalid or expired API key.");
        }

        var account = await dbContext
            .UserAccounts.AsNoTracking()
            .Include(static u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == keyEntity.UserAccountId, Context.RequestAborted);

        if (account is null || !account.IsActive || account.IsCurrentlyLockedOut(utcNow))
        {
            return AuthenticateResult.Fail("API key owner account is unavailable.");
        }

        var roles = await LoadActiveRolesAsync(account, Context.RequestAborted);
        keyEntity.RecordUsage(utcNow);
        _ = await dbContext.SaveChangesAsync(Context.RequestAborted);

        var principal = BuildPrincipal(account, roles, keyEntity.Id);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task<List<SecurityRole>> LoadActiveRolesAsync(
        UserAccount account,
        CancellationToken cancellationToken
    )
    {
        var roleIds = account.Roles.Select(static r => r.SecurityRoleId).ToList();
        return roleIds.Count == 0
            ? []
            : await dbContext
                .SecurityRoles.AsNoTracking()
                .Where(r => roleIds.Contains(r.Id) && r.IsActive)
                .ToListAsync(cancellationToken);
    }

    private ClaimsPrincipal BuildPrincipal(
        UserAccount account,
        List<SecurityRole> roles,
        RestApiKeyId apiKeyId
    )
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.Value.ToString("D")),
            new("sub", account.Id.Value.ToString("D")),
            new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Email, account.Email),
            new(RuthClaimTypes.DisplayName, account.DisplayName),
            new("auth_method", "ApiKey"),
            new("api_key_id", apiKeyId.Value.ToString("D")),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
            claims.Add(new Claim(RuthClaimTypes.RoleId, role.Id.Value.ToString("D")));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
    }

    private string? ExtractApiKeyHeader()
    {
        return Request.Headers.TryGetValue(AuthorizationTokenHeader, out var authToken)
            && !string.IsNullOrWhiteSpace(authToken.ToString())
                ? authToken.ToString().Trim()
            : Request.Headers.TryGetValue(ApiKeyHeader, out var xApiKey)
            && !string.IsNullOrWhiteSpace(xApiKey.ToString())
                ? xApiKey.ToString().Trim()
            : null;
    }
}
