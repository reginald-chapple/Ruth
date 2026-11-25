using System.Globalization;
using System.Text.Json;
using Ruth.Modules.Core.Domain.FieldTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

/// <summary>Template defining repeatable structured attribute sub-rows stored inside JSONB.</summary>
public sealed class AttributeMatrixTemplate
    : AggregateRoot<AttributeMatrixTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type key for AttributeMatrixTemplate.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.AttributeMatrixTemplate";

    private List<MatrixColumnDefinition> _columns = [];

    private AttributeMatrixTemplate() { }

    private AttributeMatrixTemplate(
        AttributeMatrixTemplateId id,
        Guid systemGuid,
        string name,
        string? description,
        int? minimumRows,
        int? maximumRows,
        string? formattedTemplate,
        List<MatrixColumnDefinition> columns
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Name = name;
        Description = description;
        IsActive = true;
        MinimumRows = minimumRows;
        MaximumRows = maximumRows;
        FormattedTemplate = formattedTemplate;
        _columns = columns;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the template display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional template description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets whether the template is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets the optional inclusive minimum number of rows.</summary>
    public int? MinimumRows { get; private set; }

    /// <summary>Gets the optional inclusive maximum number of rows.</summary>
    public int? MaximumRows { get; private set; }

    /// <summary>Gets the optional display summary template.</summary>
    public string? FormattedTemplate { get; private set; }

    /// <summary>Gets the ordered column definitions persisted as JSONB.</summary>
    public IReadOnlyList<MatrixColumnDefinition> Columns
    {
        get => _columns;
        private set => _columns = value is null ? [] : [.. value];
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a validated attribute matrix template.</summary>
    public static Result<AttributeMatrixTemplate> Create(
        IFieldTypeRegistry fieldTypes,
        string name,
        string? description,
        int? minimumRows,
        int? maximumRows,
        string? formattedTemplate,
        IReadOnlyList<MatrixColumnDefinition> columns,
        Guid? systemGuid = null
    )
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        var validation = ValidateDefinition(
            fieldTypes,
            name,
            description,
            minimumRows,
            maximumRows,
            columns
        );
        if (validation.IsFailure)
        {
            return Result.Failure<AttributeMatrixTemplate>(validation.Errors);
        }

        var id = AttributeMatrixTemplateId.Create();
        return new AttributeMatrixTemplate(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value,
            validation.Value.Name,
            validation.Value.Description,
            minimumRows,
            maximumRows,
            string.IsNullOrWhiteSpace(formattedTemplate) ? null : formattedTemplate.Trim(),
            validation.Value.Columns
        );
    }

    /// <summary>Updates this matrix template.</summary>
    public Result Update(
        IFieldTypeRegistry fieldTypes,
        string name,
        string? description,
        bool isActive,
        int? minimumRows,
        int? maximumRows,
        string? formattedTemplate,
        IReadOnlyList<MatrixColumnDefinition> columns
    )
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        var validation = ValidateDefinition(
            fieldTypes,
            name,
            description,
            minimumRows,
            maximumRows,
            columns
        );
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = validation.Value.Name;
        Description = validation.Value.Description;
        IsActive = isActive;
        MinimumRows = minimumRows;
        MaximumRows = maximumRows;
        FormattedTemplate = string.IsNullOrWhiteSpace(formattedTemplate)
            ? null
            : formattedTemplate.Trim();
        _columns = validation.Value.Columns;
        return Result.Success();
    }

    /// <summary>Validates and normalizes a JSON array of structured matrix rows against this template.</summary>
    public Result<string> ValidateAndNormalizeRows(string rawJson, IFieldTypeRegistry fieldTypes)
    {
        ArgumentNullException.ThrowIfNull(fieldTypes);

        List<Dictionary<string, string?>>? parsedRows;
        try
        {
            parsedRows = JsonSerializer.Deserialize<List<Dictionary<string, string?>>>(rawJson);
        }
        catch (JsonException)
        {
            return DomainError.Validation(
                "Core.Matrix.InvalidJson",
                "Matrix value must be a valid JSON array of row objects."
            );
        }

        if (parsedRows is null)
        {
            return DomainError.Validation("Core.Matrix.InvalidJson", "Matrix rows cannot be null.");
        }

        var boundsError = ValidateRowCountBounds(parsedRows.Count);
        return boundsError is not null
            ? (Result<string>)boundsError
            : NormalizeAllRows(parsedRows, fieldTypes);
    }

    private DomainError? ValidateRowCountBounds(int rowCount)
    {
        return MinimumRows.HasValue && rowCount < MinimumRows.Value
                ? DomainError.Validation(
                    "Core.Matrix.MinRows",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"At least {MinimumRows.Value} row(s) are required for '{Name}'."
                    )
                )
            : MaximumRows.HasValue && rowCount > MaximumRows.Value
                ? DomainError.Validation(
                    "Core.Matrix.MaxRows",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"At most {MaximumRows.Value} row(s) are allowed for '{Name}'."
                    )
                )
            : null;
    }

    private Result<string> NormalizeAllRows(
        List<Dictionary<string, string?>> parsedRows,
        IFieldTypeRegistry fieldTypes
    )
    {
        var normalizedRows = new List<Dictionary<string, string?>>(parsedRows.Count);
        for (var i = 0; i < parsedRows.Count; i++)
        {
            var rowResult = NormalizeSingleRow(parsedRows[i], i, fieldTypes);
            if (rowResult.IsFailure)
            {
                return Result.Failure<string>(rowResult.Errors);
            }

            normalizedRows.Add(rowResult.Value);
        }

        return JsonSerializer.Serialize(normalizedRows);
    }

    private Result<Dictionary<string, string?>> NormalizeSingleRow(
        Dictionary<string, string?> row,
        int rowIndex,
        IFieldTypeRegistry fieldTypes
    )
    {
        var lookup = new Dictionary<string, string?>(row, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var column in _columns.OrderBy(static c => c.Order))
        {
            if (!fieldTypes.TryGet(column.FieldTypeKey, out var plugin))
            {
                return CoreErrors.FieldTypeNotFound;
            }

            var rawValue = lookup.TryGetValue(column.Key, out var cell)
                ? cell
                : column.DefaultValue;
            var check = plugin.ValidateAndNormalize(rawValue, column.Qualifiers, column.IsRequired);
            if (check.IsFailure)
            {
                return DomainError.Validation(
                    "Core.Matrix.CellInvalid",
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Row {rowIndex + 1}, column '{column.Name}': {check.PrimaryError.Description}"
                    )
                );
            }

            normalized[column.Key] = check.Value;
        }

        return normalized;
    }

    private static Result<(
        string Name,
        string? Description,
        List<MatrixColumnDefinition> Columns
    )> ValidateDefinition(
        IFieldTypeRegistry fieldTypes,
        string name,
        string? description,
        int? minimumRows,
        int? maximumRows,
        IReadOnlyList<MatrixColumnDefinition> columns
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.MatrixTemplateNameRequired,
            CoreErrors.DefinedTypeNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var combined = Result.Combine(nameResult, descResult);
        if (combined.IsFailure)
        {
            return Result.Failure<(string, string?, List<MatrixColumnDefinition>)>(combined.Errors);
        }

        if (!AreRowBoundsValid(minimumRows, maximumRows))
        {
            return CoreErrors.MatrixTemplateRowBoundsInvalid;
        }

        var normalizedColumns = NormalizeColumns(fieldTypes, columns);
        return normalizedColumns.IsFailure
            ? Result.Failure<(string, string?, List<MatrixColumnDefinition>)>(
                normalizedColumns.Errors
            )
            : (nameResult.Value, descResult.Value, normalizedColumns.Value);
    }

    private static bool AreRowBoundsValid(int? minimumRows, int? maximumRows)
    {
        var min = minimumRows ?? 0;
        var max = maximumRows ?? int.MaxValue;
        return min >= 0 && max >= min;
    }

    private static Result<List<MatrixColumnDefinition>> NormalizeColumns(
        IFieldTypeRegistry fieldTypes,
        IReadOnlyList<MatrixColumnDefinition> columns
    )
    {
        if (columns is null || columns.Count == 0)
        {
            return CoreErrors.MatrixTemplateColumnsInvalid;
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<MatrixColumnDefinition>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
        {
            if (!TryNormalizeColumn(fieldTypes, columns[i], i, seenKeys, out var normalized))
            {
                return CoreErrors.MatrixTemplateColumnsInvalid;
            }

            result.Add(normalized);
        }

        return result;
    }

    private static bool TryNormalizeColumn(
        IFieldTypeRegistry fieldTypes,
        MatrixColumnDefinition col,
        int fallbackOrder,
        HashSet<string> seenKeys,
        out MatrixColumnDefinition normalized
    )
    {
        normalized = null!;
        var keyCheck = CoreTextPolicy.ValidateAttributeKey(col.Key);
        if (
            keyCheck.IsFailure
            || string.IsNullOrWhiteSpace(col.Name)
            || !seenKeys.Add(keyCheck.Value)
        )
        {
            return false;
        }

        if (!TryResolveColumnFieldType(fieldTypes, col.FieldTypeKey, out var pluginKey))
        {
            return false;
        }

        var qualifiers = col.Qualifiers is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(col.Qualifiers, StringComparer.OrdinalIgnoreCase);
        normalized = new MatrixColumnDefinition(
            keyCheck.Value,
            col.Name.Trim(),
            pluginKey,
            col.IsRequired,
            col.Order >= 0 ? col.Order : fallbackOrder,
            col.DefaultValue,
            qualifiers
        );
        return true;
    }

    private static bool TryResolveColumnFieldType(
        IFieldTypeRegistry fieldTypes,
        string fieldTypeKey,
        out string pluginKey
    )
    {
        if (
            fieldTypes.TryGet(fieldTypeKey, out var plugin)
            && !string.Equals(plugin.Key, "matrix", StringComparison.OrdinalIgnoreCase)
        )
        {
            pluginKey = plugin.Key;
            return true;
        }

        pluginKey = string.Empty;
        return false;
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
