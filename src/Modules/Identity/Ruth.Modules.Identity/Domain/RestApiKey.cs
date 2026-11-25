using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>SHA-256-hashed REST API key bound to a user account for non-interactive API access.</summary>
public sealed class RestApiKey : AggregateRoot<RestApiKeyId>, IAuditableEntity, IHasConcurrencyToken
{
    private RestApiKey() { }

    private RestApiKey(
        RestApiKeyId id,
        string name,
        string? description,
        string keyPrefix,
        string keyHash,
        UserAccountId userAccountId,
        DateTimeOffset? expiresOnUtc
    )
        : base(id)
    {
        Name = name;
        Description = description;
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        UserAccountId = userAccountId;
        ExpiresOnUtc = expiresOnUtc?.ToUniversalTime();
        IsActive = true;
    }

    /// <summary>Gets the friendly key label.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the non-secret key prefix shown in admin listings.</summary>
    public string KeyPrefix { get; private set; } = string.Empty;

    /// <summary>Gets the SHA-256 hex digest of the full API key.</summary>
    public string KeyHash { get; private set; } = string.Empty;

    /// <summary>Gets the bound user account identifier whose roles govern authorization.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets a value indicating whether the API key is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the optional UTC expiration timestamp.</summary>
    public DateTimeOffset? ExpiresOnUtc { get; private set; }

    /// <summary>Gets the UTC timestamp when the key was last used.</summary>
    public DateTimeOffset? LastUsedOnUtc { get; private set; }

    /// <summary>Gets the UTC timestamp when the key was revoked.</summary>
    public DateTimeOffset? RevokedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a new validated REST API key.</summary>
    public static Result<RestApiKey> Create(
        string name,
        string? description,
        string keyPrefix,
        string keyHash,
        UserAccountId userAccountId,
        DateTimeOffset? expiresOnUtc
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? (Result<RestApiKey>)IdentityErrors.ApiKeyNameRequired
            : userAccountId == UserAccountId.Empty
                ? (Result<RestApiKey>)IdentityErrors.UserAccountNotFound
            : (Result<RestApiKey>)
                new RestApiKey(
                    RestApiKeyId.Create(),
                    name.Trim(),
                    string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    keyPrefix.Trim(),
                    keyHash.Trim(),
                    userAccountId,
                    expiresOnUtc
                );
    }

    /// <summary>Determines whether the key is active, not revoked, and unexpired at <paramref name="utcNow" />.</summary>
    public bool IsValidAt(DateTimeOffset utcNow)
    {
        return IsActive
            && !RevokedOnUtc.HasValue
            && (!ExpiresOnUtc.HasValue || ExpiresOnUtc.Value > utcNow.ToUniversalTime());
    }

    /// <summary>Records key authentication usage.</summary>
    public void RecordUsage(DateTimeOffset utcNow)
    {
        LastUsedOnUtc = utcNow.ToUniversalTime();
    }

    /// <summary>Revokes the API key permanently.</summary>
    public void Revoke(DateTimeOffset utcNow)
    {
        IsActive = false;
        RevokedOnUtc = utcNow.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
