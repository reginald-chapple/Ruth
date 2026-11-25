using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal static class DefinedTypeEndpoints
{
    public static IEndpointRouteBuilder MapDefinedTypeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/defined-types")
            .WithTags("Defined Types")
            .RequireAuthorization();

        MapTypeRoutes(group);
        MapValueRoutes(group);
        return endpoints;
    }

    private static void MapTypeRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", GetAllAsync)
            .WithName("CoreGetDefinedTypes")
            .WithSummary("List DefinedTypes")
            .Produces<IReadOnlyList<DefinedTypeSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetDefinedTypeDetail")
            .WithSummary("Get DefinedType details and ordered DefinedValues")
            .Produces<DefinedTypeDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateDefinedType")
            .WithSummary("Create a new DefinedType")
            .Produces<DefinedTypeDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateDefinedType")
            .WithSummary("Update an existing DefinedType")
            .Produces<DefinedTypeDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteDefinedType")
            .WithSummary("Delete a non-system DefinedType")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapValueRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/values", AddValueAsync)
            .WithName("CoreAddDefinedValue")
            .WithSummary("Add a DefinedValue to a DefinedType")
            .Produces<DefinedValueResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}/values/{valueId:guid}", UpdateValueAsync)
            .WithName("CoreUpdateDefinedValue")
            .WithSummary("Update a DefinedValue and its JSONB attributes")
            .Produces<DefinedValueResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}/values/{valueId:guid}", DeleteValueAsync)
            .WithName("CoreDeleteDefinedValue")
            .WithSummary("Delete a non-system DefinedValue")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}/values/reorder", ReorderValuesAsync)
            .WithName("CoreReorderDefinedValues")
            .WithSummary("Reorder DefinedValues within a DefinedType")
            .Produces<IReadOnlyList<DefinedValueResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}/values/by-attribute", QueryByAttributeAsync)
            .WithName("CoreQueryDefinedValuesByAttribute")
            .WithSummary(
                "Filter DefinedValues by JSONB attribute key and value using GIN containment"
            )
            .Produces<IReadOnlyList<DefinedValueResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DefinedTypeSummaryResponse>>, ProblemHttpResult>
    > GetAllAsync(
        Guid? categoryId,
        string? search,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetDefinedTypesQuery,
            IReadOnlyList<DefinedTypeSummaryResponse>
        >(new GetDefinedTypesQuery(categoryId, search, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<DefinedTypeDetailResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetDefinedTypeDetailQuery,
            DefinedTypeDetailResponse
        >(new GetDefinedTypeDetailQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<DefinedTypeDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateDefinedTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateDefinedTypeCommand,
            DefinedTypeDetailResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/defined-types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<DefinedTypeDetailResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateDefinedTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateDefinedTypeCommand,
            DefinedTypeDetailResponse
        >(command with { DefinedTypeId = id }, cancellationToken);
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
        var result = await dispatcher.SendAsync<DeleteDefinedTypeCommand, bool>(
            new DeleteDefinedTypeCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<DefinedValueResponse>, ProblemHttpResult>
    > AddValueAsync(
        Guid id,
        AddDefinedValueCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddDefinedValueCommand, DefinedValueResponse>(
            command with
            {
                DefinedTypeId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri(
                    $"/api/core/defined-types/{id:D}/values/{result.Value.Id:D}",
                    UriKind.Relative
                ),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<DefinedValueResponse>, ProblemHttpResult>
    > UpdateValueAsync(
        Guid id,
        Guid valueId,
        UpdateDefinedValueCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateDefinedValueCommand, DefinedValueResponse>(
            command with
            {
                DefinedTypeId = id,
                DefinedValueId = valueId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteValueAsync(
        Guid id,
        Guid valueId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteDefinedValueCommand, bool>(
            new DeleteDefinedValueCommand(id, valueId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DefinedValueResponse>>, ProblemHttpResult>
    > ReorderValuesAsync(
        Guid id,
        IReadOnlyList<Guid> orderedValueIds,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ReorderDefinedValuesCommand,
            IReadOnlyList<DefinedValueResponse>
        >(new ReorderDefinedValuesCommand(id, orderedValueIds), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DefinedValueResponse>>, ProblemHttpResult>
    > QueryByAttributeAsync(
        Guid id,
        string key,
        string value,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            QueryDefinedValuesByAttributeQuery,
            IReadOnlyList<DefinedValueResponse>
        >(new QueryDefinedValuesByAttributeQuery(id, key, value), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
