namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Represents the currently authenticated user account and role assignments.</summary>
public sealed record AuthenticatedUserResponse(
    Guid UserAccountId,
    string UserName,
    string Email,
    string DisplayName,
    bool IsConfirmed,
    bool IsLockedOut,
    bool IsActive,
    bool IsPasswordChangeRequired,
    DateTimeOffset? LastLoginOnUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> RoleIds
);
