namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents an OAuth/OIDC scope definition.</summary>
public sealed record AuthScopeResponse(
    Guid Id,
    string Name,
    string PublicName,
    bool IsSystem,
    bool IsActive
);
