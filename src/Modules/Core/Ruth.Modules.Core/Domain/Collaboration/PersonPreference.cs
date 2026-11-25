using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Key-value user/person preference optionally scoped to an entity with enduring retention policy.</summary>
public sealed class PersonPreference : AggregateRoot<PersonPreferenceId>, IAuditableEntity
{
    /// <summary>Retention window in months for standard (non-enduring) preferences.</summary>
    public const int StandardRetentionMonths = 2;

    /// <summary>Retention window in months for enduring preferences.</summary>
    public const int EnduringRetentionMonths = 18;

    private PersonPreference() { }

    private PersonPreference(
        PersonPreferenceId id,
        Guid ownerKey,
        string key,
        string value,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        bool isEnduring,
        DateTimeOffset nowUtc
    )
        : base(id)
    {
        OwnerKey = ownerKey;
        Key = key;
        Value = value;
        EntityTypeId = entityTypeId;
        EntityId = entityId;
        IsEnduring = isEnduring;
        LastAccessedOnUtc = nowUtc;
    }

    /// <summary>Gets the owner identifier (UserAccountId, PersonId, or anonymous visitor token).</summary>
    public Guid OwnerKey { get; private set; }

    /// <summary>Gets the preference key (max 250 chars).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the optional entity type scope.</summary>
    public EntityTypeDescriptorId? EntityTypeId { get; private set; }

    /// <summary>Gets the optional entity instance scope.</summary>
    public Guid? EntityId { get; private set; }

    /// <summary>Gets the stored preference string value.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Gets whether this preference has extended 18-month retention.</summary>
    public bool IsEnduring { get; private set; }

    /// <summary>Gets when this preference was last accessed or updated.</summary>
    public DateTimeOffset LastAccessedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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

    /// <summary>Returns whether this preference has exceeded its retention window as of <paramref name="nowUtc" />.</summary>
    public bool IsExpiredRetention(DateTimeOffset nowUtc)
    {
        var months = IsEnduring ? EnduringRetentionMonths : StandardRetentionMonths;
        return LastAccessedOnUtc.AddMonths(months) <= nowUtc;
    }

    /// <summary>Updates the preference value, enduring flag, and access timestamp.</summary>
    public void UpdateValue(
        string value,
        bool isEnduring,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        DateTimeOffset nowUtc
    )
    {
        Value = value ?? string.Empty;
        IsEnduring = isEnduring;
        EntityTypeId = entityTypeId;
        EntityId = entityId;
        LastAccessedOnUtc = nowUtc;
    }

    /// <summary>Creates a validated person preference entry.</summary>
    public static Result<PersonPreference> Create(
        Guid ownerKey,
        string key,
        string value,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        bool isEnduring,
        DateTimeOffset nowUtc
    )
    {
        return string.IsNullOrWhiteSpace(key) || key.Trim().Length > 250
            ? (Result<PersonPreference>)CoreCollaborationErrors.PreferenceKeyInvalid
            : (Result<PersonPreference>)
                new PersonPreference(
                    PersonPreferenceId.Create(),
                    ownerKey,
                    key.Trim(),
                    value ?? string.Empty,
                    entityTypeId,
                    entityId,
                    isEnduring,
                    nowUtc
                );
    }
}
