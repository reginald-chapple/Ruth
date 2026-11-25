using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.AttributeMatrices;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.AttributeMatrices;

internal static class AttributeMatrixEndpoints
{
    public static IEndpointRouteBuilder MapAttributeMatrixEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/attribute-matrices")
            .WithTags("Attribute Matrices")
            .RequireAuthorization();

        _ = group
            .MapGet("/", GetAllAsync)
            .WithName("CoreGetAttributeMatrixTemplates")
            .WithSummary("List attribute matrix templates")
            .Produces<IReadOnlyList<AttributeMatrixTemplateResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetAttributeMatrixTemplateById")
            .WithSummary("Get an attribute matrix template by ID")
            .Produces<AttributeMatrixTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateAttributeMatrixTemplate")
            .WithSummary("Create a new attribute matrix template")
            .Produces<AttributeMatrixTemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateAttributeMatrixTemplate")
            .WithSummary("Update an attribute matrix template")
            .Produces<AttributeMatrixTemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteAttributeMatrixTemplate")
            .WithSummary("Delete an attribute matrix template")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AttributeMatrixTemplateResponse>>, ProblemHttpResult>
    > GetAllAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttributeMatrixTemplatesQuery,
            IReadOnlyList<AttributeMatrixTemplateResponse>
        >(new GetAttributeMatrixTemplatesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeMatrixTemplateResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAttributeMatrixTemplateByIdQuery,
            AttributeMatrixTemplateResponse
        >(new GetAttributeMatrixTemplateByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<AttributeMatrixTemplateResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateAttributeMatrixTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateAttributeMatrixTemplateCommand,
            AttributeMatrixTemplateResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/attribute-matrices/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AttributeMatrixTemplateResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateAttributeMatrixTemplateCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateAttributeMatrixTemplateCommand,
            AttributeMatrixTemplateResponse
        >(command with { TemplateId = id }, cancellationToken);
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
        var result = await dispatcher.SendAsync<DeleteAttributeMatrixTemplateCommand, bool>(
            new DeleteAttributeMatrixTemplateCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
