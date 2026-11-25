namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Issued JWT access token, rotating refresh token, and authenticated principal summary.</summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresOnUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresOnUtc,
    Guid UserAccountId,
    string UserName,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> RoleIds,
    bool IsPasswordChangeRequired
);
