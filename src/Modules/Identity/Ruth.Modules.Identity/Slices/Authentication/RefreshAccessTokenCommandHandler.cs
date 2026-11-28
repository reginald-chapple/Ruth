using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class RefreshAccessTokenCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<RefreshAccessTokenCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return IdentityErrors.RefreshTokenInvalid;
        }

        var utcNow = clock.UtcNow;
        var hash = tokenService.ComputeSha256Hash(command.RefreshToken);
        var existingToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash,
            cancellationToken
        );

        var ambientTenantId = AmbientTenantContext.CurrentTenantId;
        if (
            existingToken is null
            || (!ambientTenantId.IsEmpty && existingToken.TenantId != ambientTenantId)
        )
        {
            return IdentityErrors.RefreshTokenInvalid;
        }

        if (existingToken.IsRevoked)
        {
            await RevokeTokenFamilyAndAuditReuseAsync(
                existingToken,
                command,
                utcNow,
                cancellationToken
            );
            return IdentityErrors.RefreshTokenInvalid;
        }

        if (!existingToken.IsActiveAt(utcNow))
        {
            return IdentityErrors.RefreshTokenInvalid;
        }

        var account = await dbContext
            .UserAccounts.Include(static u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserAccountId, cancellationToken);

        if (
            account is null
            || !account.IsActive
            || !account.IsConfirmed
            || account.IsCurrentlyLockedOut(utcNow)
        )
        {
            existingToken.Revoke(utcNow, "AccountUnavailable");
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return IdentityErrors.RefreshTokenInvalid;
        }

        return await RotateTokenAsync(existingToken, account, command, utcNow, cancellationToken);
    }

    private async Task<AuthTokensResponse> RotateTokenAsync(
        RefreshToken existingToken,
        UserAccount account,
        RefreshAccessTokenCommand command,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var roles = await AuthTokenIssuer.LoadActiveRolesAsync(
            dbContext,
            account,
            cancellationToken
        );
        var (accessToken, accessExpires) = tokenService.CreateAccessToken(account, roles, utcNow);
        var rawNextRefresh = tokenService.GenerateOpaqueToken(byteLength: 32);
        var nextExpires = utcNow.ToUniversalTime().Add(tokenService.RefreshTokenLifetime);
        var nextRefresh = RefreshToken.Create(
            account.Id,
            tokenService.ComputeSha256Hash(rawNextRefresh),
            existingToken.FamilyId,
            utcNow,
            nextExpires,
            command.IpAddress
        );

        existingToken.Revoke(utcNow, "Rotated", nextRefresh.Id);
        _ = dbContext.RefreshTokens.Add(nextRefresh);
        _ = dbContext.LoginHistoryEntries.Add(
            LoginHistoryEntry.Record(
                account.Id,
                account.UserName,
                "RefreshToken",
                succeeded: true,
                utcNow,
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(
            accessToken,
            accessExpires,
            rawNextRefresh,
            nextExpires,
            account.Id.Value,
            account.UserName,
            account.Email,
            account.DisplayName,
            [.. roles.Select(static r => r.Name)],
            [.. roles.Select(static r => r.Id.Value)],
            account.IsPasswordChangeRequired
        );
    }

    private async Task RevokeTokenFamilyAndAuditReuseAsync(
        RefreshToken reusedToken,
        RefreshAccessTokenCommand command,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var familyTokens = await dbContext
            .RefreshTokens.Where(t => t.FamilyId == reusedToken.FamilyId && t.RevokedOnUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in familyTokens)
        {
            token.Revoke(utcNow, "RefreshTokenReuseDetected");
        }

        var userName = await dbContext
            .UserAccounts.AsNoTracking()
            .Where(u => u.Id == reusedToken.UserAccountId)
            .Select(static u => u.UserName)
            .FirstOrDefaultAsync(cancellationToken);

        _ = dbContext.LoginHistoryEntries.Add(
            LoginHistoryEntry.Record(
                reusedToken.UserAccountId,
                userName ?? "revoked-refresh-token",
                "RefreshToken",
                succeeded: false,
                utcNow,
                failureReason: "RefreshTokenReuseDetected",
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent
            )
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
