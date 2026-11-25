using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Individual lookup item within a <see cref="DefinedType" /> supporting extensible JSONB attributes.</summary>
public sealed class DefinedValue
    : Entity<DefinedValueId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for DefinedValue.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.DefinedValue";

    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private DefinedValue() { }

    internal DefinedValue(
        DefinedValueId id,
        DefinedTypeId definedTypeId,
        Guid systemGuid,
        string value,
        string? description,
        CategoryId? categoryId,
        int order,
        bool isSystem
    )
        : base(id)
    {
        DefinedTypeId = definedTypeId;
        SystemGuid = systemGuid;
        Value = value;
        Description = description;
        CategoryId = categoryId;
        Order = order;
        IsSystem = isSystem;
        IsActive = true;
    }

    /// <summary>Gets the owning DefinedType identifier.</summary>
    public DefinedTypeId DefinedTypeId { get; private set; }

    /// <summary>Gets the stable external/system GUID for this value.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the display value text.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional category identifier when categorized values are enabled.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets the zero-based display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this value is built-in and protected from deletion.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets whether this value is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the JSONB attribute map persisted by EF Core.</summary>
    public IReadOnlyDictionary<string, string?> Attributes
    {
        get => _attributes;
        private set =>
            _attributes = value is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> AttributeValues => _attributes;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            DefinedType.EntityTypeKeyName,
            DefinedTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    internal Result Update(
        string value,
        string? description,
        CategoryId? categoryId,
        int order,
        bool isActive
    )
    {
        var valueResult = CoreTextPolicy.ValidateRequiredText(
            value,
            CoreTextPolicy.DefinedValueMaxLength,
            CoreErrors.DefinedValueRequired,
            CoreErrors.DefinedValueTooLong
        );
        var descriptionResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(valueResult, descriptionResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Value = valueResult.Value;
        Description = descriptionResult.Value;
        CategoryId = categoryId;
        Order = Math.Max(0, order);
        IsActive = isActive;
        return Result.Success();
    }

    internal void SetOrder(int order)
    {
        Order = Math.Max(0, order);
    }

    /// <inheritdoc />
    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var next = new Dictionary<string, string?>(_attributes, StringComparer.OrdinalIgnoreCase)
        {
            [key.Trim()] = value,
        };
        _attributes = next;
    }

    /// <inheritdoc />
    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var next = new Dictionary<string, string?>(_attributes, StringComparer.OrdinalIgnoreCase);
        _ = next.Remove(key.Trim());
        _attributes = next;
    }

    /// <summary>Replaces all stored attribute values cleanly for EF Core change tracking.</summary>
    public void ReplaceAttributes(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var next = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                next[pair.Key.Trim()] = pair.Value;
            }
        }

        _attributes = next;
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
