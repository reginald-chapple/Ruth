using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Lookup dictionary aggregate owning ordered <see cref="DefinedValue" /> items.</summary>
public sealed class DefinedType
    : AggregateRoot<DefinedTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for DefinedType.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.DefinedType";

    private readonly List<DefinedValue> _values = [];

    private DefinedType() { }

    private DefinedType(
        DefinedTypeId id,
        Guid systemGuid,
        string name,
        string? description,
        string? helpText,
        CategoryId? categoryId,
        int order,
        bool isSystem,
        bool enableSecurityOnValues,
        bool categorizedValuesEnabled
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        HelpText = helpText;
        CategoryId = categoryId;
        Order = order;
        IsSystem = isSystem;
        IsActive = true;
        EnableSecurityOnValues = enableSecurityOnValues;
        CategorizedValuesEnabled = categorizedValuesEnabled;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase name for uniqueness enforcement.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets optional help text for staff configuring values.</summary>
    public string? HelpText { get; private set; }

    /// <summary>Gets the optional category identifier.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this defined type is built-in and protected from deletion.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets whether this defined type is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether per-value security ACLs are enabled.</summary>
    public bool EnableSecurityOnValues { get; private set; }

    /// <summary>Gets whether values can be grouped into categories.</summary>
    public bool CategorizedValuesEnabled { get; private set; }

    /// <summary>Gets the child DefinedValues.</summary>
    public IReadOnlyCollection<DefinedValue> Values => _values.AsReadOnly();

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
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated DefinedType aggregate.</summary>
    public static Result<DefinedType> Create(
        string name,
        string? description = null,
        string? helpText = null,
        CategoryId? categoryId = null,
        int order = 0,
        bool isSystem = false,
        bool enableSecurityOnValues = false,
        bool categorizedValuesEnabled = false,
        Guid? systemGuid = null
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.DefinedTypeNameRequired,
            CoreErrors.DefinedTypeNameTooLong
        );
        var descriptionResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var helpTextResult = CoreTextPolicy.ValidateOptionalText(
            helpText,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descriptionResult, helpTextResult);
        if (combined.IsFailure)
        {
            return Result.Failure<DefinedType>(combined.Errors);
        }

        var id = DefinedTypeId.Create();
        return new DefinedType(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value,
            nameResult.Value,
            descriptionResult.Value,
            helpTextResult.Value,
            categoryId,
            Math.Max(0, order),
            isSystem,
            enableSecurityOnValues,
            categorizedValuesEnabled
        );
    }

    /// <summary>Updates DefinedType metadata.</summary>
    public Result Update(
        string name,
        string? description,
        string? helpText,
        CategoryId? categoryId,
        int order,
        bool isActive,
        bool enableSecurityOnValues,
        bool categorizedValuesEnabled
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.DefinedTypeNameRequired,
            CoreErrors.DefinedTypeNameTooLong
        );
        var descriptionResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var helpTextResult = CoreTextPolicy.ValidateOptionalText(
            helpText,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descriptionResult, helpTextResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        NormalizedName = nameResult.Value.ToUpperInvariant();
        Description = descriptionResult.Value;
        HelpText = helpTextResult.Value;
        CategoryId = categoryId;
        Order = Math.Max(0, order);
        IsActive = isActive;
        EnableSecurityOnValues = enableSecurityOnValues;
        CategorizedValuesEnabled = categorizedValuesEnabled;
        return Result.Success();
    }

    /// <summary>Adds a new unique DefinedValue to this DefinedType.</summary>
    public Result<DefinedValue> AddValue(
        string value,
        string? description = null,
        CategoryId? categoryId = null,
        int? order = null,
        bool isSystem = false,
        Guid? systemGuid = null
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
            return Result.Failure<DefinedValue>(combined.Errors);
        }

        if (
            _values.Exists(v =>
                string.Equals(v.Value, valueResult.Value, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return CoreErrors.DefinedValueDuplicate;
        }

        var nextOrder = order ?? (_values.Count == 0 ? 0 : _values.Max(static v => v.Order) + 1);
        var itemId = DefinedValueId.Create();
        var item = new DefinedValue(
            itemId,
            Id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : itemId.Value,
            valueResult.Value,
            descriptionResult.Value,
            categoryId,
            Math.Max(0, nextOrder),
            isSystem
        );
        _values.Add(item);
        return item;
    }

    /// <summary>Updates an existing DefinedValue item.</summary>
    public Result<DefinedValue> UpdateValue(
        DefinedValueId valueId,
        string value,
        string? description,
        CategoryId? categoryId,
        int order,
        bool isActive
    )
    {
        var item = _values.Find(v => v.Id == valueId);
        if (item is null)
        {
            return CoreErrors.DefinedValueNotFound;
        }

        var trimmed = value?.Trim() ?? string.Empty;
        if (
            _values.Exists(v =>
                v.Id != valueId
                && string.Equals(v.Value, trimmed, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return CoreErrors.DefinedValueDuplicate;
        }

        var updated = item.Update(value ?? string.Empty, description, categoryId, order, isActive);
        return updated.IsFailure ? Result.Failure<DefinedValue>(updated.Errors) : item;
    }

    /// <summary>Removes a non-system DefinedValue item.</summary>
    public Result RemoveValue(DefinedValueId valueId)
    {
        var item = _values.Find(v => v.Id == valueId);
        if (item is null)
        {
            return CoreErrors.DefinedValueNotFound;
        }

        if (item.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        _ = _values.Remove(item);
        return Result.Success();
    }

    /// <summary>Reorders child values according to the provided ID order.</summary>
    public void ReorderValues(IReadOnlyList<Guid> orderedValueIds)
    {
        ArgumentNullException.ThrowIfNull(orderedValueIds);
        var orderMap = new Dictionary<Guid, int>();
        for (var i = 0; i < orderedValueIds.Count; i++)
        {
            _ = orderMap.TryAdd(orderedValueIds[i], i);
        }

        var fallbackOrder = orderedValueIds.Count;
        foreach (var item in _values.OrderBy(static v => v.Order))
        {
            item.SetOrder(
                orderMap.TryGetValue(item.Id.Value, out var explicitOrder)
                    ? explicitOrder
                    : fallbackOrder++
            );
        }
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
