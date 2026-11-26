using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Internal SaaS Platform Operator account strictly isolated from organization UserAccount tables.</summary>
public sealed class PlatformOperator
    : AggregateRoot<PlatformOperatorId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private PlatformOperator() { }

    private PlatformOperator(
        PlatformOperatorId id,
        string emailNormalized,
        string displayName,
        string passwordHash,
        DateTimeOffset createdOnUtc
    )
        : base(id)
    {
        EmailNormalized = emailNormalized;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public string EmailNormalized { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public static PlatformOperator Create(
        string email,
        string displayName,
        string passwordHash,
        DateTimeOffset nowUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new PlatformOperator(
            PlatformOperatorId.Create(),
            email.Trim().ToLowerInvariant(),
            displayName.Trim(),
            passwordHash,
            nowUtc
        );
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
