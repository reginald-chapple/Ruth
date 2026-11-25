using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Recursive hierarchical category aggregate scoped to an entity type and optional qualifier.</summary>
public sealed class Category
    : AggregateRoot<CategoryId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for Category.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Category";

    private Category() { }

    private Category(
        CategoryId id,
        Guid systemGuid,
        EntityTypeDescriptorId entityTypeId,
        string? qualifierColumn,
        string? qualifierValue,
        CategoryId? parentCategoryId,
        string name,
        string? description,
        string? iconName,
        string? highlightColor,
        int order,
        bool isSystem
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        EntityTypeId = entityTypeId;
        EntityTypeQualifierColumn = qualifierColumn;
        EntityTypeQualifierValue = qualifierValue;
        ParentCategoryId = parentCategoryId;
        Name = name;
        Description = description;
        IconName = iconName;
        HighlightColor = highlightColor;
        Order = order;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the entity type descriptor identifier this category organizes.</summary>
    public EntityTypeDescriptorId EntityTypeId { get; private set; }

    /// <summary>Gets the optional qualifier property name narrowing category scope.</summary>
    public string? EntityTypeQualifierColumn { get; private set; }

    /// <summary>Gets the optional qualifier value narrowing category scope.</summary>
    public string? EntityTypeQualifierValue { get; private set; }

    /// <summary>Gets the parent category identifier, or null for root categories.</summary>
    public CategoryId? ParentCategoryId { get; private set; }

    /// <summary>Gets the category display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional category description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional icon name or CSS class.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the optional highlight hex color.</summary>
    public string? HighlightColor { get; private set; }

    /// <summary>Gets the sibling display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this category is built-in and protected from deletion.</summary>
    public bool IsSystem { get; private set; }

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
        ParentCategoryId.HasValue
            ? new SecuredEntityRef(
                EntityTypeKeyName,
                ParentCategoryId.Value.Value,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated category node.</summary>
    public static Result<Category> Create(
        EntityTypeDescriptorId entityTypeId,
        string name,
        CategoryId? parentCategoryId = null,
        string? qualifierColumn = null,
        string? qualifierValue = null,
        string? description = null,
        string? iconName = null,
        string? highlightColor = null,
        int order = 0,
        bool isSystem = false,
        Guid? systemGuid = null
    )
    {
        if (entityTypeId == EntityTypeDescriptorId.Empty)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.CategoryNameRequired,
            CoreErrors.CategoryNameTooLong
        );
        var descriptionResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var iconResult = CoreTextPolicy.ValidateOptionalText(
            iconName,
            maxLength: 100,
            allowNewLines: false
        );
        var colorResult = CoreTextPolicy.ValidateOptionalText(
            highlightColor,
            maxLength: 50,
            allowNewLines: false
        );

        var combined = Result.Combine(nameResult, descriptionResult, iconResult, colorResult);
        if (combined.IsFailure)
        {
            return Result.Failure<Category>(combined.Errors);
        }

        var id = CategoryId.Create();
        return new Category(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value,
            entityTypeId,
            string.IsNullOrWhiteSpace(qualifierColumn) ? null : qualifierColumn.Trim(),
            string.IsNullOrWhiteSpace(qualifierValue) ? null : qualifierValue.Trim(),
            parentCategoryId,
            nameResult.Value,
            descriptionResult.Value,
            iconResult.Value,
            colorResult.Value,
            Math.Max(0, order),
            isSystem
        );
    }

    /// <summary>Updates mutable category details.</summary>
    public Result Update(
        string name,
        string? description,
        string? iconName,
        string? highlightColor,
        int order
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreErrors.CategoryNameRequired,
            CoreErrors.CategoryNameTooLong
        );
        var descriptionResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var iconResult = CoreTextPolicy.ValidateOptionalText(
            iconName,
            maxLength: 100,
            allowNewLines: false
        );
        var colorResult = CoreTextPolicy.ValidateOptionalText(
            highlightColor,
            maxLength: 50,
            allowNewLines: false
        );

        var combined = Result.Combine(nameResult, descriptionResult, iconResult, colorResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        Description = descriptionResult.Value;
        IconName = iconResult.Value;
        HighlightColor = colorResult.Value;
        Order = Math.Max(0, order);
        return Result.Success();
    }

    /// <summary>Changes the parent category after external ancestor-cycle validation.</summary>
    public Result MoveTo(CategoryId? newParentCategoryId, int? newOrder = null)
    {
        if (newParentCategoryId.HasValue && newParentCategoryId.Value == Id)
        {
            return CoreErrors.CategoryCycleDetected;
        }

        ParentCategoryId = newParentCategoryId;
        if (newOrder.HasValue)
        {
            Order = Math.Max(0, newOrder.Value);
        }

        return Result.Success();
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
