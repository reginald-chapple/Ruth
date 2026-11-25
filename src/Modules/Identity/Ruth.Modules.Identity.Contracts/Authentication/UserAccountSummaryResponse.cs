namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Summary representation of a user account and its assigned security roles.</summary>
public sealed record UserAccountSummaryResponse(
    Guid UserAccountId,
    string UserName,
    string Email,
    string DisplayName,
    bool IsConfirmed,
    bool IsLockedOut,
    bool IsActive,
    DateTimeOffset? LastLoginOnUtc,
    DateTimeOffset CreatedOnUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> RoleIds
);
