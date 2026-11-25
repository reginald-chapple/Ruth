using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Extensible attribute schema definition scoped globally, by entity type, or by entity qualifier.</summary>
public sealed class AttributeDefinition
    : AggregateRoot<AttributeDefinitionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for AttributeDefinition.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.AttributeDefinition";

    private Dictionary<string, string> _qualifiers = new(StringComparer.OrdinalIgnoreCase);

    private AttributeDefinition() { }

    private AttributeDefinition(
        AttributeDefinitionId id,
        Guid systemGuid,
        EntityTypeDescriptorId? entityTypeId,
        string? qualifierColumn,
        string? qualifierValue,
        string key,
        string name,
        string? description,
        string fieldTypeKey,
        bool isRequired,
        bool isGridColumn,
        bool isMultiValue,
        bool isSystem,
        string? defaultValue,
        string? globalValue,
        int order,
        CategoryId? categoryId,
        Dictionary<string, string> qualifiers
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        EntityTypeId = entityTypeId;
        EntityTypeQualifierColumn = qualifierColumn;
        EntityTypeQualifierValue = qualifierValue;
        Key = key;
        NormalizedKey = key.ToUpperInvariant();
        Name = name;
        Description = description;
        FieldTypeKey = fieldTypeKey;
        IsRequired = isRequired;
        IsGridColumn = isGridColumn;
        IsMultiValue = isMultiValue;
        IsSystem = isSystem;
        IsActive = true;
        DefaultValue = defaultValue;
        GlobalValue = globalValue;
        Order = order;
        CategoryId = categoryId;
        _qualifiers = qualifiers;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the optional owning entity type identifier (null indicates a Global Attribute).</summary>
    public EntityTypeDescriptorId? EntityTypeId { get; private set; }

    /// <summary>Gets the optional qualifier property name narrowing entity scope.</summary>
    public string? EntityTypeQualifierColumn { get; private set; }

    /// <summary>Gets the optional qualifier value narrowing entity scope.</summary>
    public string? EntityTypeQualifierValue { get; private set; }

    /// <summary>Gets the programmatic attribute key.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase attribute key for uniqueness checks.</summary>
    public string NormalizedKey { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable attribute label.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional attribute help description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the lowercase <see cref="IFieldTypePlugin.Key" /> governing validation and rendering.</summary>
    public string FieldTypeKey { get; private set; } = string.Empty;

    /// <summary>Gets whether a value is required when editing an entity.</summary>
    public bool IsRequired { get; private set; }

    /// <summary>Gets whether this attribute appears as a column in entity data grids.</summary>
    public bool IsGridColumn { get; private set; }

    /// <summary>Gets whether multiple values are supported.</summary>
    public bool IsMultiValue { get; private set; }

    /// <summary>Gets whether this attribute is built-in and protected from deletion.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets whether this attribute is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the optional normalized default value.</summary>
    public string? DefaultValue { get; private set; }

    /// <summary>Gets the stored value when this definition is a Global Attribute.</summary>
    public string? GlobalValue { get; private set; }

    /// <summary>Gets the display sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets the optional category identifier.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets the JSONB qualifier configuration map.</summary>
    public IReadOnlyDictionary<string, string> Qualifiers
    {
        get => _qualifiers;
        private set =>
            _qualifiers = value is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets a value indicating whether this definition is a Global Attribute.</summary>
    public bool IsGlobal => !EntityTypeId.HasValue;

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

    /// <summary>Creates a validated attribute definition verified against <paramref name="fieldTypes" />.</summary>
    public static Result<AttributeDefinition> Create(
        IFieldTypeRegistry fieldTypes,
        EntityTypeDescriptorId? entityTypeId,
        string? qualifierColumn,
        string? qualifierValue,
        string key,
        string name,
        string? description,
        string fieldTypeKey,
        bool isRequired = false,
        bool isGridColumn = false,
        bool isMultiValue = false,
        bool isSystem = false,
        string? defaultValue = null,
        string? globalValue = null,
        int order = 0,
        CategoryId? categoryId = null,
        IReadOnlyDictionary<string, string>? qualifiers = null,
        Guid? systemGuid = null
    )
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        var validated = ValidateCreateMetadata(key, name, description);
        if (validated.IsFailure)
        {
            return Result.Failure<AttributeDefinition>(validated.Errors);
        }

        var (validKey, validName, validDesc) = validated.Value;
        if (!fieldTypes.TryGet(fieldTypeKey, out var plugin))
        {
            return CoreErrors.FieldTypeNotFound;
        }

        var normalizedQualifiers = NormalizeQualifiers(qualifiers);
        var defaultCheck = plugin.ValidateAndNormalize(
            defaultValue,
            normalizedQualifiers,
            isRequired: false
        );
        var globalCheck = ValidateInitialGlobalValue(
            plugin,
            entityTypeId,
            globalValue,
            normalizedQualifiers,
            isRequired
        );
        var valueCheck = Result.Combine(defaultCheck, globalCheck);
        if (valueCheck.IsFailure)
        {
            return Result.Failure<AttributeDefinition>(valueCheck.Errors);
        }

        var id = AttributeDefinitionId.Create();
        var effectiveSystemGuid =
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value;
        return new AttributeDefinition(
            id,
            effectiveSystemGuid,
            entityTypeId,
            TrimOrNull(qualifierColumn),
            TrimOrNull(qualifierValue),
            validKey,
            validName,
            validDesc,
            plugin.Key,
            isRequired,
            isGridColumn,
            isMultiValue,
            isSystem,
            defaultCheck.Value,
            globalCheck.Value,
            Math.Max(0, order),
            categoryId,
            normalizedQualifiers
        );
    }

    private static Result<(string Key, string Name, string? Description)> ValidateCreateMetadata(
        string key,
        string name,
        string? description
    )
    {
        var keyResult = CoreTextPolicy.ValidateAttributeKey(key);
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.AttributeNameRequired,
            CoreErrors.DefinedTypeNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var baseCheck = Result.Combine(keyResult, nameResult, descResult);
        return baseCheck.IsFailure
            ? Result.Failure<(string, string, string?)>(baseCheck.Errors)
            : (keyResult.Value, nameResult.Value, descResult.Value);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Result<string?> ValidateInitialGlobalValue(
        IFieldTypePlugin plugin,
        EntityTypeDescriptorId? entityTypeId,
        string? globalValue,
        Dictionary<string, string> qualifiers,
        bool isRequired
    )
    {
        return entityTypeId.HasValue || globalValue is null
            ? Result.Success<string?>(value: null)
            : plugin.ValidateAndNormalize(globalValue, qualifiers, isRequired);
    }

    /// <summary>Updates mutable attribute definition properties.</summary>
    public Result Update(
        IFieldTypeRegistry fieldTypes,
        string name,
        string? description,
        string fieldTypeKey,
        bool isRequired,
        bool isGridColumn,
        bool isMultiValue,
        bool isActive,
        string? defaultValue,
        int order,
        CategoryId? categoryId,
        IReadOnlyDictionary<string, string>? qualifiers
    )
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.AttributeNameRequired,
            CoreErrors.DefinedTypeNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        if (!fieldTypes.TryGet(fieldTypeKey, out var plugin))
        {
            return CoreErrors.FieldTypeNotFound;
        }

        var normalizedQualifiers = NormalizeQualifiers(qualifiers);
        var defaultCheck = plugin.ValidateAndNormalize(
            defaultValue,
            normalizedQualifiers,
            isRequired: false
        );
        if (defaultCheck.IsFailure)
        {
            return defaultCheck;
        }

        Name = nameResult.Value;
        Description = descResult.Value;
        FieldTypeKey = plugin.Key;
        IsRequired = isRequired;
        IsGridColumn = isGridColumn;
        IsMultiValue = isMultiValue;
        IsActive = isActive;
        DefaultValue = defaultCheck.Value;
        Order = Math.Max(0, order);
        CategoryId = categoryId;
        _qualifiers = normalizedQualifiers;
        return Result.Success();
    }

    /// <summary>Sets the stored global attribute value after field-type validation.</summary>
    public Result SetGlobalValue(IFieldTypeRegistry fieldTypes, string? rawValue)
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        if (!IsGlobal)
        {
            return CoreErrors.AttributeNotGlobal;
        }

        if (!fieldTypes.TryGet(FieldTypeKey, out var plugin))
        {
            return CoreErrors.FieldTypeNotFound;
        }

        var validated = plugin.ValidateAndNormalize(rawValue, _qualifiers, IsRequired);
        if (validated.IsFailure)
        {
            return validated;
        }

        GlobalValue = validated.Value;
        return Result.Success();
    }

    private static Dictionary<string, string> NormalizeQualifiers(
        IReadOnlyDictionary<string, string>? qualifiers
    )
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (qualifiers is null)
        {
            return map;
        }

        foreach (var pair in qualifiers)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null)
            {
                map[pair.Key.Trim()] = pair.Value.Trim();
            }
        }

        return map;
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
