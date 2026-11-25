using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class CoreCategoryAndAttributeIntegrationTests(CorePostgresContainerFixture postgres)
{
    [Fact]
    public async Task Categories_RecursiveTreeMoveAndCyclePrevention_WorkAgainstPostgres18()
    {
        var cs = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();

        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var entityType = await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
            new GetEntityTypeByKeyQuery(DefinedType.EntityTypeKeyName)
        );
        Assert.True(entityType.IsSuccess);

        var root = await commands.SendAsync<CreateCategoryCommand, CategoryResponse>(
            new CreateCategoryCommand(
                entityType.Value.Id,
                "Ministries",
                Description: "Top-level ministries",
                HighlightColor: "#0078D4",
                Order: 0
            )
        );
        var child = await commands.SendAsync<CreateCategoryCommand, CategoryResponse>(
            new CreateCategoryCommand(
                entityType.Value.Id,
                "NextGen",
                ParentCategoryId: root.Value.Id,
                Order: 0
            )
        );
        var grandChild = await commands.SendAsync<CreateCategoryCommand, CategoryResponse>(
            new CreateCategoryCommand(
                entityType.Value.Id,
                "Students",
                ParentCategoryId: child.Value.Id,
                Order: 0
            )
        );
        Assert.True(root.IsSuccess);
        Assert.True(child.IsSuccess);
        Assert.True(grandChild.IsSuccess);

        var cycleMove = await commands.SendAsync<MoveCategoryCommand, CategoryResponse>(
            new MoveCategoryCommand(root.Value.Id, grandChild.Value.Id, NewOrder: 0)
        );
        Assert.Equal(CoreErrors.CategoryCycleDetected.Code, cycleMove.PrimaryError.Code);

        var deleteParent = await commands.SendAsync<DeleteCategoryCommand, bool>(
            new DeleteCategoryCommand(child.Value.Id)
        );
        Assert.Equal(CoreErrors.CategoryHasChildren.Code, deleteParent.PrimaryError.Code);

        var tree = await queries.QueryAsync<
            GetCategoryTreeQuery,
            IReadOnlyList<CategoryTreeNodeResponse>
        >(new GetCategoryTreeQuery(entityType.Value.Id));
        Assert.True(tree.IsSuccess);
        var rootNode = Assert.Single(tree.Value);
        Assert.Equal("Ministries", rootNode.Name);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal("NextGen", childNode.Name);
        var grandChildNode = Assert.Single(childNode.Children);
        Assert.Equal("Students", grandChildNode.Name);
    }

    [Fact]
    public async Task AttributesAndMatrices_GlobalValuesAndValidationPipeline_WorkAgainstPostgres18()
    {
        var cs = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();

        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var globalAttr = await commands.SendAsync<
            CreateAttributeDefinitionCommand,
            AttributeDefinitionResponse
        >(
            new CreateAttributeDefinitionCommand(
                EntityTypeId: null,
                EntityTypeQualifierColumn: null,
                EntityTypeQualifierValue: null,
                Key: "orgWebsite",
                Name: "Organization Website",
                Description: "Public homepage",
                FieldTypeKey: "url",
                IsRequired: true,
                DefaultValue: "https://ruthrms.org"
            )
        );
        Assert.True(globalAttr.IsSuccess);

        var setGlobal = await commands.SendAsync<
            SetGlobalAttributeValueCommand,
            AttributeDefinitionResponse
        >(new SetGlobalAttributeValueCommand(globalAttr.Value.Id, "https://gracefellowship.org"));
        Assert.True(setGlobal.IsSuccess);
        Assert.Equal("https://gracefellowship.org/", setGlobal.Value.GlobalValue);

        var matrixTemplate = await CreateCertificationsMatrixAsync(commands);
        var dvType = await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
            new GetEntityTypeByKeyQuery(DefinedValue.EntityTypeKeyName)
        );

        var matrixAttr = await commands.SendAsync<
            CreateAttributeDefinitionCommand,
            AttributeDefinitionResponse
        >(
            new CreateAttributeDefinitionCommand(
                EntityTypeId: dvType.Value.Id,
                EntityTypeQualifierColumn: null,
                EntityTypeQualifierValue: null,
                Key: "certifications",
                Name: "Certifications",
                Description: null,
                FieldTypeKey: "matrix",
                IsRequired: true,
                Order: 1,
                Qualifiers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["attributeMatrixTemplateId"] = matrixTemplate.Id.ToString("D"),
                }
            )
        );
        Assert.True(matrixAttr.IsSuccess);

        var validCheck = await commands.SendAsync<
            ValidateAttributeValuesCommand,
            AttributeValidationResponse
        >(
            new ValidateAttributeValuesCommand(
                DefinedValue.EntityTypeKeyName,
                QualifierColumn: null,
                QualifierValue: null,
                Values: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["certifications"] =
                        /*lang=json,strict*/
                        "[{\"title\":\"Child Safety\",\"completedOn\":\"2026-05-01\"}]",
                }
            )
        );
        Assert.True(validCheck.IsSuccess);
        Assert.True(validCheck.Value.IsValid);
        Assert.Equal("1 row", validCheck.Value.FormattedValues["certifications"]);

        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        Assert.Equal(1, await db.AttributeMatrixTemplates.CountAsync());
        Assert.Equal(2, await db.AttributeDefinitions.CountAsync());
    }

    private static async Task<AttributeMatrixTemplateResponse> CreateCertificationsMatrixAsync(
        ICommandDispatcher commands
    )
    {
        var matrixTemplate = await commands.SendAsync<
            CreateAttributeMatrixTemplateCommand,
            AttributeMatrixTemplateResponse
        >(
            new CreateAttributeMatrixTemplateCommand(
                Name: "Certifications",
                Description: "Volunteer certifications",
                MinimumRows: 1,
                MaximumRows: 5,
                FormattedTemplate: "{{ title }}",
                Columns:
                [
                    new MatrixColumnDefinitionDto(
                        "title",
                        "Title",
                        "text",
                        IsRequired: true,
                        Order: 0,
                        DefaultValue: null,
                        Qualifiers: null
                    ),
                    new MatrixColumnDefinitionDto(
                        "completedOn",
                        "Completed On",
                        "date",
                        IsRequired: true,
                        Order: 1,
                        DefaultValue: null,
                        Qualifiers: null
                    ),
                ]
            )
        );
        Assert.True(matrixTemplate.IsSuccess);
        return matrixTemplate.Value;
    }
}
