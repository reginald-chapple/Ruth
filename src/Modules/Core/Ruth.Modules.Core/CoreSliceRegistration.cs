using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.Modules.Core.Slices.AttributeMatrices;
using Ruth.Modules.Core.Slices.Attributes;
using Ruth.Modules.Core.Slices.Categories;
using Ruth.Modules.Core.Slices.DefinedTypes;
using Ruth.Modules.Core.Slices.EntityTypes;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Core;

internal static class CoreSliceRegistration
{
    public static void RegisterEntityAndDefinedTypeHandlers(IServiceCollection services)
    {
        RegisterEntityTypeHandlers(services);
        RegisterDefinedTypeHandlers(services);
    }

    public static void RegisterCategoryAndAttributeHandlers(IServiceCollection services)
    {
        RegisterCategoryHandlers(services);
        RegisterAttributeHandlers(services);
    }

    private static void RegisterEntityTypeHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetEntityTypesQuery,
            IReadOnlyList<EntityTypeResponse>,
            GetEntityTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEntityTypeByKeyQuery,
            EntityTypeResponse,
            GetEntityTypeByKeyQueryHandler
        >();
    }

    private static void RegisterDefinedTypeHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetDefinedTypesQuery,
            IReadOnlyList<DefinedTypeSummaryResponse>,
            GetDefinedTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetDefinedTypeDetailQuery,
            DefinedTypeDetailResponse,
            GetDefinedTypeDetailQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateDefinedTypeCommand,
            DefinedTypeDetailResponse,
            CreateDefinedTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateDefinedTypeCommand,
            DefinedTypeDetailResponse,
            UpdateDefinedTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteDefinedTypeCommand,
            bool,
            DeleteDefinedTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddDefinedValueCommand,
            DefinedValueResponse,
            AddDefinedValueCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateDefinedValueCommand,
            DefinedValueResponse,
            UpdateDefinedValueCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteDefinedValueCommand,
            bool,
            DeleteDefinedValueCommandHandler
        >();
        _ = services.AddCommandHandler<
            ReorderDefinedValuesCommand,
            IReadOnlyList<DefinedValueResponse>,
            ReorderDefinedValuesCommandHandler
        >();
        _ = services.AddQueryHandler<
            QueryDefinedValuesByAttributeQuery,
            IReadOnlyList<DefinedValueResponse>,
            QueryDefinedValuesByAttributeQueryHandler
        >();
    }

    private static void RegisterCategoryHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetCategoryTreeQuery,
            IReadOnlyList<CategoryTreeNodeResponse>,
            GetCategoryTreeQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCategoryByIdQuery,
            CategoryResponse,
            GetCategoryByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateCategoryCommand,
            CategoryResponse,
            CreateCategoryCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCategoryCommand,
            CategoryResponse,
            UpdateCategoryCommandHandler
        >();
        _ = services.AddCommandHandler<
            MoveCategoryCommand,
            CategoryResponse,
            MoveCategoryCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteCategoryCommand, bool, DeleteCategoryCommandHandler>();
    }

    private static void RegisterAttributeHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetFieldTypesQuery,
            IReadOnlyList<FieldTypeResponse>,
            GetFieldTypesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAttributeDefinitionsQuery,
            IReadOnlyList<AttributeDefinitionResponse>,
            GetAttributeDefinitionsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAttributeDefinitionByIdQuery,
            AttributeDefinitionResponse,
            GetAttributeDefinitionByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAttributeDefinitionCommand,
            AttributeDefinitionResponse,
            CreateAttributeDefinitionCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateAttributeDefinitionCommand,
            AttributeDefinitionResponse,
            UpdateAttributeDefinitionCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteAttributeDefinitionCommand,
            bool,
            DeleteAttributeDefinitionCommandHandler
        >();
        _ = services.AddCommandHandler<
            SetGlobalAttributeValueCommand,
            AttributeDefinitionResponse,
            SetGlobalAttributeValueCommandHandler
        >();
        _ = services.AddCommandHandler<
            ValidateAttributeValuesCommand,
            AttributeValidationResponse,
            ValidateAttributeValuesCommandHandler
        >();
    }

    public static void RegisterMatrixHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetAttributeMatrixTemplatesQuery,
            IReadOnlyList<AttributeMatrixTemplateResponse>,
            GetAttributeMatrixTemplatesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAttributeMatrixTemplateByIdQuery,
            AttributeMatrixTemplateResponse,
            GetAttributeMatrixTemplateByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAttributeMatrixTemplateCommand,
            AttributeMatrixTemplateResponse,
            CreateAttributeMatrixTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateAttributeMatrixTemplateCommand,
            AttributeMatrixTemplateResponse,
            UpdateAttributeMatrixTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteAttributeMatrixTemplateCommand,
            bool,
            DeleteAttributeMatrixTemplateCommandHandler
        >();
    }
}
