using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>OAuth/OIDC authorization scope definition.</summary>
public sealed class AuthScope : AggregateRoot<AuthScopeId>, IAuditableEntity, IHasConcurrencyToken
{
    private AuthScope() { }

    private AuthScope(AuthScopeId id, string name, string publicName, bool isSystem)
        : base(id)
    {
        Name = name;
        PublicName = publicName;
        IsSystem = isSystem;
        IsActive = true;
    }

    /// <summary>Gets the unique scope key (e.g., openid, profile, email).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the user-facing display label.</summary>
    public string PublicName { get; private set; } = string.Empty;

    /// <summary>Gets whether this scope is built-in.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets whether this scope is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a validated OAuth/OIDC scope.</summary>
    public static Result<AuthScope> Create(string name, string publicName, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 50)
        {
            return IdentityErrors.AuthScopeInvalid;
        }

        var trimmedName = name.Trim().ToLowerInvariant();
        var trimmedPublic = string.IsNullOrWhiteSpace(publicName) ? trimmedName : publicName.Trim();

        return new AuthScope(AuthScopeId.Create(), trimmedName, trimmedPublic, isSystem);
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
