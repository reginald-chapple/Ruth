using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Categories;

internal static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        _ = group
            .MapGet("/tree", GetTreeAsync)
            .WithName("CoreGetCategoryTree")
            .WithSummary(
                "Get recursive category tree filtered by entity type and optional qualifier"
            )
            .Produces<IReadOnlyList<CategoryTreeNodeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetCategoryById")
            .WithSummary("Get a single category by ID")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateCategory")
            .WithSummary("Create a new category")
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateCategory")
            .WithSummary("Update category details")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/move", MoveAsync)
            .WithName("CoreMoveCategory")
            .WithSummary("Reparent a category with ancestor cycle protection")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteCategory")
            .WithSummary("Delete a non-system leaf category")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CategoryTreeNodeResponse>>, ProblemHttpResult>
    > GetTreeAsync(
        Guid? entityTypeId,
        string? entityTypeKey,
        string? qualifierColumn,
        string? qualifierValue,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCategoryTreeQuery,
            IReadOnlyList<CategoryTreeNodeResponse>
        >(
            new GetCategoryTreeQuery(entityTypeId, entityTypeKey, qualifierColumn, qualifierValue),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetCategoryByIdQuery, CategoryResponse>(
            new GetCategoryByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<CategoryResponse>, ProblemHttpResult>> CreateAsync(
        CreateCategoryCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateCategoryCommand, CategoryResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/categories/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateCategoryCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateCategoryCommand, CategoryResponse>(
            command with
            {
                CategoryId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<CategoryResponse>, ProblemHttpResult>> MoveAsync(
        Guid id,
        MoveCategoryCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<MoveCategoryCommand, CategoryResponse>(
            command with
            {
                CategoryId = id,
            },
            cancellationToken
        );
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
        var result = await dispatcher.SendAsync<DeleteCategoryCommand, bool>(
            new DeleteCategoryCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
