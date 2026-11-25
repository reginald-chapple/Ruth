using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Structured domain history/audit timeline entry with sensitive-value masking.</summary>
public sealed class HistoryLogEntry : AggregateRoot<HistoryLogEntryId>, IAuditableEntity, ISecured
{
    /// <summary>Canonical entity type key for HistoryLogEntry.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.HistoryLogEntry";

    /// <summary>Standard masked summary text for sensitive field changes.</summary>
    public const string SensitiveMaskSummary = "(Sensitive values are not logged in history)";

    private HistoryLogEntry() { }

    private HistoryLogEntry(
        HistoryLogEntryId id,
        EntityTypeDescriptorId entityTypeId,
        Guid entityId,
        CategoryId? categoryId,
        string verb,
        string changeType,
        string? caption,
        string? valueName,
        string? oldValue,
        string? newValue,
        bool isSensitive,
        EntityTypeDescriptorId? relatedEntityTypeId,
        Guid? relatedEntityId,
        string? relatedData,
        string? sourceOfChange,
        Guid? changedByUserId,
        DateTimeOffset occurredOnUtc
    )
        : base(id)
    {
        EntityTypeId = entityTypeId;
        EntityId = entityId;
        CategoryId = categoryId;
        Verb = verb;
        ChangeType = changeType;
        Caption = caption;
        ValueName = valueName;
        OldValue = isSensitive ? null : oldValue;
        NewValue = isSensitive ? null : newValue;
        IsSensitive = isSensitive;
        RelatedEntityTypeId = relatedEntityTypeId;
        RelatedEntityId = relatedEntityId;
        RelatedData = relatedData;
        SourceOfChange = sourceOfChange;
        ChangedByUserId = changedByUserId;
        OccurredOnUtc = occurredOnUtc;
    }

    /// <summary>Gets the target entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId EntityTypeId { get; private set; }

    /// <summary>Gets the target entity identifier.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the optional history category identifier.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets the domain verb (e.g., ADD, MODIFY, DELETE, MERGE, LOGIN).</summary>
    public string Verb { get; private set; } = string.Empty;

    /// <summary>Gets the change kind (Record, Property, Attribute).</summary>
    public string ChangeType { get; private set; } = "Property";

    /// <summary>Gets the optional target entity caption.</summary>
    public string? Caption { get; private set; }

    /// <summary>Gets the changed property or attribute display name.</summary>
    public string? ValueName { get; private set; }

    /// <summary>Gets the previous value (null when <see cref="IsSensitive" /> is true).</summary>
    public string? OldValue { get; private set; }

    /// <summary>Gets the updated value (null when <see cref="IsSensitive" /> is true).</summary>
    public string? NewValue { get; private set; }

    /// <summary>Gets whether old/new values were suppressed for sensitivity.</summary>
    public bool IsSensitive { get; private set; }

    /// <summary>Gets the optional related entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId? RelatedEntityTypeId { get; private set; }

    /// <summary>Gets the optional related entity identifier.</summary>
    public Guid? RelatedEntityId { get; private set; }

    /// <summary>Gets optional structured related context.</summary>
    public string? RelatedData { get; private set; }

    /// <summary>Gets optional source of change (UI page, API, Job).</summary>
    public string? SourceOfChange { get; private set; }

    /// <summary>Gets the user who initiated the change.</summary>
    public Guid? ChangedByUserId { get; private set; }

    /// <summary>Gets when the change occurred in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

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

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    Guid ISecured.EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        CategoryId.HasValue
            ? new SecuredEntityRef(
                Category.EntityTypeKeyName,
                CategoryId.Value.Value,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Formats a human-readable summary string honoring sensitive-value masking.</summary>
    public string FormatSummary()
    {
        if (IsSensitive)
        {
            return string.IsNullOrWhiteSpace(ValueName)
                ? $"{Verb} {SensitiveMaskSummary}"
                : $"{Verb} {ValueName}: {SensitiveMaskSummary}";
        }

        if (!string.IsNullOrWhiteSpace(ValueName))
        {
            if (OldValue is null && NewValue is not null)
            {
                return $"Added {ValueName} '{NewValue}'.";
            }

            if (OldValue is not null && NewValue is null)
            {
                return $"Removed {ValueName} '{OldValue}'.";
            }

            if (OldValue is not null && NewValue is not null)
            {
                return $"Modified {ValueName} from '{OldValue}' to '{NewValue}'.";
            }
        }

        return string.IsNullOrWhiteSpace(Caption) ? Verb : $"{Verb} {Caption}";
    }

    /// <summary>Creates a validated history log entry.</summary>
    public static Result<HistoryLogEntry> Create(
        EntityTypeDescriptorId entityTypeId,
        Guid entityId,
        string verb,
        string changeType,
        string? caption,
        string? valueName,
        string? oldValue,
        string? newValue,
        bool isSensitive,
        CategoryId? categoryId,
        EntityTypeDescriptorId? relatedEntityTypeId,
        Guid? relatedEntityId,
        string? relatedData,
        string? sourceOfChange,
        Guid? changedByUserId,
        DateTimeOffset occurredOnUtc
    )
    {
        if (entityTypeId == EntityTypeDescriptorId.Empty)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        if (string.IsNullOrWhiteSpace(verb) || verb.Trim().Length > 50)
        {
            return CoreCollaborationErrors.HistoryVerbRequired;
        }

        var normalizedChangeType = string.IsNullOrWhiteSpace(changeType)
            ? "Property"
            : changeType.Trim();
        if (normalizedChangeType.Length > 20)
        {
            normalizedChangeType = normalizedChangeType[..20];
        }

        return new HistoryLogEntry(
            HistoryLogEntryId.Create(),
            entityTypeId,
            entityId,
            categoryId,
            verb.Trim().ToUpperInvariant(),
            normalizedChangeType,
            Normalize(caption, 200),
            Normalize(valueName, 250),
            oldValue?.Trim(),
            newValue?.Trim(),
            isSensitive,
            relatedEntityTypeId,
            relatedEntityId,
            Normalize(relatedData, 500),
            Normalize(sourceOfChange, 200),
            changedByUserId,
            occurredOnUtc
        );
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
