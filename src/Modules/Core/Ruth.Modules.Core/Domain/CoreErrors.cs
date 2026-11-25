using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

internal static class CoreErrors
{
    public static readonly DomainError InvalidControlCharacters = DomainError.Validation(
        "Core.InvalidControlCharacters",
        "Input text cannot contain null bytes or control characters."
    );

    public static readonly DomainError EntityTypeNotFound = DomainError.NotFound(
        "Core.EntityTypeNotFound",
        "Entity type was not found."
    );

    public static readonly DomainError EntityTypeInvalid = DomainError.Validation(
        "Core.EntityTypeInvalid",
        "Entity type key, name, and module name are required."
    );

    public static readonly DomainError DefinedTypeNameRequired = DomainError.Validation(
        "Core.DefinedTypeNameRequired",
        "Defined type name is required."
    );

    public static readonly DomainError DefinedTypeNameTooLong = DomainError.Validation(
        "Core.DefinedTypeNameTooLong",
        "Defined type name cannot exceed 100 characters."
    );

    public static readonly DomainError DefinedTypeNotFound = DomainError.NotFound(
        "Core.DefinedTypeNotFound",
        "Defined type was not found."
    );

    public static readonly DomainError DefinedTypeAlreadyExists = DomainError.Conflict(
        "Core.DefinedTypeAlreadyExists",
        "A defined type with that name already exists."
    );

    public static readonly DomainError DefinedValueRequired = DomainError.Validation(
        "Core.DefinedValueRequired",
        "Defined value text is required."
    );

    public static readonly DomainError DefinedValueTooLong = DomainError.Validation(
        "Core.DefinedValueTooLong",
        "Defined value text cannot exceed 250 characters."
    );

    public static readonly DomainError DefinedValueDuplicate = DomainError.Conflict(
        "Core.DefinedValueDuplicate",
        "A defined value with that text already exists in this defined type."
    );

    public static readonly DomainError DefinedValueNotFound = DomainError.NotFound(
        "Core.DefinedValueNotFound",
        "Defined value was not found."
    );

    public static readonly DomainError SystemItemProtected = DomainError.Forbidden(
        "Core.SystemItemProtected",
        "Built-in system records cannot be deleted."
    );

    public static readonly DomainError CategoryNameRequired = DomainError.Validation(
        "Core.CategoryNameRequired",
        "Category name is required."
    );

    public static readonly DomainError CategoryNameTooLong = DomainError.Validation(
        "Core.CategoryNameTooLong",
        "Category name cannot exceed 100 characters."
    );

    public static readonly DomainError CategoryNotFound = DomainError.NotFound(
        "Core.CategoryNotFound",
        "Category was not found."
    );

    public static readonly DomainError CategoryCycleDetected = DomainError.Validation(
        "Core.CategoryCycleDetected",
        "Moving this category would create a circular parent-child hierarchy."
    );

    public static readonly DomainError CategoryEntityTypeMismatch = DomainError.Validation(
        "Core.CategoryEntityTypeMismatch",
        "Parent category must belong to the same entity type."
    );

    public static readonly DomainError CategoryHasChildren = DomainError.Conflict(
        "Core.CategoryHasChildren",
        "Cannot delete a category that still has child categories."
    );

    public static readonly DomainError AttributeKeyInvalid = DomainError.Validation(
        "Core.AttributeKeyInvalid",
        "Attribute key must start with a letter and contain only letters, digits, underscores, hyphens, or dots (max 100 chars)."
    );

    public static readonly DomainError AttributeNameRequired = DomainError.Validation(
        "Core.AttributeNameRequired",
        "Attribute name is required."
    );

    public static readonly DomainError AttributeNotFound = DomainError.NotFound(
        "Core.AttributeNotFound",
        "Attribute definition was not found."
    );

    public static readonly DomainError AttributeDuplicateKey = DomainError.Conflict(
        "Core.AttributeDuplicateKey",
        "An attribute with that key already exists for the specified entity scope."
    );

    public static readonly DomainError AttributeNotGlobal = DomainError.Validation(
        "Core.AttributeNotGlobal",
        "Only global attributes can store a direct global value."
    );

    public static readonly DomainError FieldTypeNotFound = DomainError.Validation(
        "Core.FieldTypeNotFound",
        "The specified field type key is not registered."
    );

    public static readonly DomainError MatrixTemplateNameRequired = DomainError.Validation(
        "Core.MatrixTemplateNameRequired",
        "Attribute matrix template name is required."
    );

    public static readonly DomainError MatrixTemplateNotFound = DomainError.NotFound(
        "Core.MatrixTemplateNotFound",
        "Attribute matrix template was not found."
    );

    public static readonly DomainError MatrixTemplateRowBoundsInvalid = DomainError.Validation(
        "Core.MatrixTemplateRowBoundsInvalid",
        "Minimum rows must be non-negative and cannot exceed maximum rows."
    );

    public static readonly DomainError MatrixTemplateColumnsInvalid = DomainError.Validation(
        "Core.MatrixTemplateColumnsInvalid",
        "Attribute matrix template must contain unique, valid column keys and registered field types."
    );
}
