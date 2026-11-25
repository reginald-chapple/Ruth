using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Attributes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Attributes;

internal static class AttributeEndpoints
{
    public static IEndpointRouteBuilder MapAttributeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/attributes")
            .WithTags("Attributes")
            .RequireAuthorization();

        _ = group
            .MapGet("/", GetAllAsync)
            .WithName("CoreGetAttributes")
            .WithSummary("List global or entity-scoped attribute definitions")
            .Produces<IReadOnlyList<AttributeDefinitionResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetAttributeById")
            .WithSummary("Get a single attribute definition")
            .Produces<AttributeDefinitionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateAttribute")
            .WithSummary("Create a global or entity-scoped attribute definition")
            .Produces<AttributeDefinitionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateAttribute")
            .WithSummary("Update an attribute definition")
            .Produces<AttributeDefinitionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}/global-value", SetGlobalValueAsync)
            .WithName("CoreSetGlobalAttributeValue")
            .WithSummary("Set and validate a global attribute value")
            .Produces<AttributeDefinitionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteAttribute")
            .WithSummary("Delete a non-system attribute definition")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/validate", ValidateValuesAsync)
            .WithName("CoreValidateAttributeValues")
            .WithSummary(
                "Validate and format attribute values against entity attribute definitions"
            )
            .Produces<AttributeValidationResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AttributeDefinitionResponse>>, ProblemHttpResult>
    > GetAllAsync(
        Guid? entityTypeId,
        string? entityTypeKey,
        string? qualifierColumn,
        string? qualifierValue,
        bool? globalOnly,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttributeDefinitionsQuery,
            IReadOnlyList<AttributeDefinitionResponse>
        >(
            new GetAttributeDefinitionsQuery(
                entityTypeId,
                entityTypeKey,
                qualifierColumn,
                qualifierValue,
                globalOnly ?? false,
                activeOnly ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeDefinitionResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttributeDefinitionByIdQuery,
            AttributeDefinitionResponse
        >(new GetAttributeDefinitionByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AttributeDefinitionResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateAttributeDefinitionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateAttributeDefinitionCommand,
            AttributeDefinitionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/attributes/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeDefinitionResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateAttributeDefinitionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateAttributeDefinitionCommand,
            AttributeDefinitionResponse
        >(command with { AttributeDefinitionId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeDefinitionResponse>, ProblemHttpResult>
    > SetGlobalValueAsync(
        Guid id,
        SetGlobalAttributeValueCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetGlobalAttributeValueCommand,
            AttributeDefinitionResponse
        >(command with { AttributeDefinitionId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteAttributeDefinitionCommand, bool>(
            new DeleteAttributeDefinitionCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeValidationResponse>, ProblemHttpResult>
    > ValidateValuesAsync(
        ValidateAttributeValuesCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ValidateAttributeValuesCommand,
            AttributeValidationResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
