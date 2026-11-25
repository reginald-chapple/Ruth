namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents a security role and its active member count.</summary>
public sealed record SecurityRoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int MemberCount,
    DateTimeOffset CreatedOnUtc
);
