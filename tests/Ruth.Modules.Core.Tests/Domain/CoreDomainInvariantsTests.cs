using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.FieldTypes;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreDomainInvariantsTests
{
    private static readonly FieldTypeRegistry _fieldTypes = new(BuiltInFieldTypes.CreateAll());

    [Fact]
    public void CategoryTreeValidator_CoversRootValidMoveSelfCycleDeepCycleMissingAndEntityTypeMismatch()
    {
        var typeA = EntityTypeDescriptorId.Create();
        var typeB = EntityTypeDescriptorId.Create();
        var rootId = CategoryId.Create();
        var childId = CategoryId.Create();
        var grandChildId = CategoryId.Create();
        var otherTypeId = CategoryId.Create();

        var nodes = new Dictionary<
            CategoryId,
            (CategoryId? ParentId, EntityTypeDescriptorId EntityTypeId)
        >
        {
            [rootId] = (null, typeA),
            [childId] = (rootId, typeA),
            [grandChildId] = (childId, typeA),
            [otherTypeId] = (null, typeB),
        };

        Assert.True(
            CategoryTreeValidator
                .ValidateParentAssignment(grandChildId, typeA, newParentId: null, nodes)
                .IsSuccess
        );
        Assert.True(
            CategoryTreeValidator
                .ValidateParentAssignment(grandChildId, typeA, rootId, nodes)
                .IsSuccess
        );

        var selfResult = CategoryTreeValidator.ValidateParentAssignment(
            rootId,
            typeA,
            rootId,
            nodes
        );
        Assert.Equal(CoreErrors.CategoryCycleDetected.Code, selfResult.PrimaryError.Code);

        var cycleResult = CategoryTreeValidator.ValidateParentAssignment(
            rootId,
            typeA,
            grandChildId,
            nodes
        );
        Assert.Equal(CoreErrors.CategoryCycleDetected.Code, cycleResult.PrimaryError.Code);

        var missingResult = CategoryTreeValidator.ValidateParentAssignment(
            childId,
            typeA,
            CategoryId.Create(),
            nodes
        );
        Assert.Equal(CoreErrors.CategoryNotFound.Code, missingResult.PrimaryError.Code);

        var mismatchResult = CategoryTreeValidator.ValidateParentAssignment(
            childId,
            typeA,
            otherTypeId,
            nodes
        );
        Assert.Equal(CoreErrors.CategoryEntityTypeMismatch.Code, mismatchResult.PrimaryError.Code);
    }

    [Fact]
    public void DefinedType_AddUpdateReorderRemoveAndSystemProtection_EnforcesInvariants()
    {
        var created = DefinedType.Create(
            "Connection Status",
            description: "Member connection statuses",
            isSystem: true,
            order: 0
        );
        Assert.True(created.IsSuccess);
        var definedType = created.Value;

        var first = definedType.AddValue(
            "Member",
            description: "Active member",
            order: 0,
            isSystem: true
        );
        var second = definedType.AddValue(
            "Visitor",
            description: "First-time guest",
            order: 1,
            isSystem: false
        );
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var duplicate = definedType.AddValue(
            "member",
            description: null,
            order: 2,
            isSystem: false
        );
        Assert.Equal(CoreErrors.DefinedValueDuplicate.Code, duplicate.PrimaryError.Code);

        second.Value.ReplaceAttributes(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["badgeColor"] = "#0078D4",
                ["priority"] = "2",
            }
        );
        Assert.Equal("#0078D4", second.Value.Attributes["badgeColor"]);

        definedType.ReorderValues([second.Value.Id.Value, first.Value.Id.Value]);
        Assert.Equal(0, second.Value.Order);
        Assert.Equal(1, first.Value.Order);

        var removeSystem = definedType.RemoveValue(first.Value.Id);
        Assert.Equal(CoreErrors.SystemItemProtected.Code, removeSystem.PrimaryError.Code);
        Assert.True(definedType.RemoveValue(second.Value.Id).IsSuccess);
    }

    [Fact]
    public void AttributeMatrixTemplate_RejectsInvalidBoundsAndNestedMatrixColumns()
    {
        var validColumns = CreateLineItemColumns();

        var badBounds = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "Invalid",
            description: null,
            minimumRows: 5,
            maximumRows: 2,
            formattedTemplate: null,
            columns: validColumns
        );
        Assert.True(badBounds.IsFailure);

        var nestedMatrix = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "Nested",
            description: null,
            minimumRows: 0,
            maximumRows: 5,
            formattedTemplate: null,
            columns:
            [
                new MatrixColumnDefinition(
                    "inner",
                    "Inner",
                    "matrix",
                    IsRequired: false,
                    Order: 0,
                    DefaultValue: null,
                    Qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                ),
            ]
        );
        Assert.True(nestedMatrix.IsFailure);

        var emptyCols = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "Empty",
            description: null,
            minimumRows: 0,
            maximumRows: 5,
            formattedTemplate: null,
            columns: []
        );
        Assert.True(emptyCols.IsFailure);

        var duplicateCols = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "Duplicate",
            description: null,
            minimumRows: null,
            maximumRows: null,
            formattedTemplate: null,
            columns:
            [
                new MatrixColumnDefinition(
                    "dup",
                    "Dup 1",
                    "text",
                    IsRequired: false,
                    Order: -1,
                    DefaultValue: null,
                    Qualifiers: null!
                ),
                new MatrixColumnDefinition(
                    "DUP",
                    "Dup 2",
                    "text",
                    IsRequired: false,
                    Order: 1,
                    DefaultValue: null,
                    Qualifiers: null!
                ),
            ]
        );
        Assert.True(duplicateCols.IsFailure);

        var unknownFieldCol = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "UnknownType",
            description: null,
            minimumRows: -1,
            maximumRows: 5,
            formattedTemplate: null,
            columns: validColumns
        );
        Assert.True(unknownFieldCol.IsFailure);
    }

    [Fact]
    public void AttributeMatrixTemplate_ValidatesAndNormalizesRowsAndSupportsUpdates()
    {
        var columns = CreateLineItemColumns();
        var created = AttributeMatrixTemplate.Create(
            _fieldTypes,
            "Line Items",
            description: "Structured order items",
            minimumRows: 1,
            maximumRows: 2,
            formattedTemplate: "{{ itemName }}",
            columns: columns
        );
        Assert.True(created.IsSuccess);
        var template = created.Value;

        Assert.True(template.ValidateAndNormalizeRows("not-json", _fieldTypes).IsFailure);
        Assert.True(template.ValidateAndNormalizeRows("null", _fieldTypes).IsFailure);
        Assert.True(template.ValidateAndNormalizeRows("[]", _fieldTypes).IsFailure);
        Assert.True(
            template
                .ValidateAndNormalizeRows(
                    /*lang=json,strict*/
                    "[{\"itemName\":\"A\"},{\"itemName\":\"B\"},{\"itemName\":\"C\"}]",
                    _fieldTypes
                )
                .IsFailure
        );
        Assert.True(
            template
                .ValidateAndNormalizeRows( /*lang=json,strict*/
                    "[{\"itemName\":\"A\",\"qty\":\"0\"}]",
                    _fieldTypes
                )
                .IsFailure
        );

        var okRows = template.ValidateAndNormalizeRows(
            /*lang=json,strict*/
            "[{\"itemName\":\"  Widget  \"}]",
            _fieldTypes
        );
        Assert.True(okRows.IsSuccess);
        Assert.Contains("\"itemName\":\"Widget\"", okRows.Value, StringComparison.Ordinal);
        Assert.Contains("\"qty\":\"1\"", okRows.Value, StringComparison.Ordinal);

        var updated = template.Update(
            _fieldTypes,
            "Line Items v2",
            description: "Updated",
            isActive: true,
            minimumRows: 0,
            maximumRows: 5,
            formattedTemplate: null,
            columns: columns
        );
        Assert.True(updated.IsSuccess);
        Assert.Equal("Line Items v2", template.Name);
    }

    [Fact]
    public void AttributeValueValidator_ValidatesDefaultsFormattingErrorsAndMatrixTemplates()
    {
        var template = CreateContactsTemplate();
        var definitions = CreateSampleDefinitions(template.Id.Value);
        var templates = new Dictionary<Guid, AttributeMatrixTemplate>
        {
            [template.Id.Value] = template,
        };

        var validResult = AttributeValueValidator.Validate(
            definitions,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["age"] = "30",
                ["contacts"] = /*lang=json,strict*/
                    "[{\"contactName\":\"Jane Doe\"}]",
            },
            _fieldTypes,
            templates
        );

        Assert.True(validResult.IsValid);
        Assert.Equal("Guest", validResult.NormalizedValues["nickname"]);
        Assert.Equal("30", validResult.NormalizedValues["age"]);
        Assert.Equal("1 row", validResult.FormattedValues["contacts"]);

        var invalidResult = AttributeValueValidator.Validate(
            definitions,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["age"] = "not-int",
                ["contacts"] = "[]",
            },
            _fieldTypes,
            templates
        );

        Assert.False(invalidResult.IsValid);
        Assert.True(invalidResult.FieldErrors.ContainsKey("age"));
        Assert.True(invalidResult.FieldErrors.ContainsKey("contacts"));
    }

    private static MatrixColumnDefinition[] CreateLineItemColumns()
    {
        return
        [
            new MatrixColumnDefinition(
                "itemName",
                "Item Name",
                "text",
                IsRequired: true,
                Order: 0,
                DefaultValue: null,
                Qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            ),
            new MatrixColumnDefinition(
                "qty",
                "Quantity",
                "integer",
                IsRequired: true,
                Order: 1,
                DefaultValue: "1",
                Qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["minValue"] = "1",
                }
            ),
        ];
    }

    private static AttributeMatrixTemplate CreateContactsTemplate()
    {
        return AttributeMatrixTemplate
            .Create(
                _fieldTypes,
                "Emergency Contacts",
                description: null,
                minimumRows: 1,
                maximumRows: 3,
                formattedTemplate: null,
                columns:
                [
                    new MatrixColumnDefinition(
                        "contactName",
                        "Contact Name",
                        "text",
                        IsRequired: true,
                        Order: 0,
                        DefaultValue: null,
                        Qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    ),
                ]
            )
            .Value;
    }

    private static AttributeDefinition[] CreateSampleDefinitions(Guid matrixTemplateId)
    {
        var nameDef = AttributeDefinition
            .Create(
                _fieldTypes,
                entityTypeId: null,
                qualifierColumn: null,
                qualifierValue: null,
                key: "nickname",
                name: "Nickname",
                description: null,
                fieldTypeKey: "text",
                isRequired: false,
                defaultValue: "Guest",
                isSystem: false,
                order: 0,
                qualifiers: null
            )
            .Value;

        var ageDef = AttributeDefinition
            .Create(
                _fieldTypes,
                entityTypeId: null,
                qualifierColumn: null,
                qualifierValue: null,
                key: "age",
                name: "Age",
                description: null,
                fieldTypeKey: "integer",
                isRequired: true,
                defaultValue: null,
                isSystem: false,
                order: 1,
                qualifiers: null
            )
            .Value;

        var matrixDef = AttributeDefinition
            .Create(
                _fieldTypes,
                entityTypeId: null,
                qualifierColumn: null,
                qualifierValue: null,
                key: "contacts",
                name: "Contacts",
                description: null,
                fieldTypeKey: "matrix",
                isRequired: true,
                defaultValue: null,
                isSystem: false,
                order: 2,
                qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attributeMatrixTemplateId"] = matrixTemplateId.ToString("D"),
                }
            )
            .Value;

        return [nameDef, ageDef, matrixDef];
    }
}
