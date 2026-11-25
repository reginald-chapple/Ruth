namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Represents metadata for a REST API key (excluding the secret value).</summary>
public sealed record RestApiKeyResponse(
    Guid Id,
    string Name,
    string? Description,
    string KeyPrefix,
    Guid UserAccountId,
    string UserName,
    bool IsActive,
    DateTimeOffset? ExpiresOnUtc,
    DateTimeOffset? LastUsedOnUtc,
    DateTimeOffset? RevokedOnUtc,
    DateTimeOffset CreatedOnUtc
);
