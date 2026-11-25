using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Files;

internal static class BinaryFileTypeEndpoints
{
    public static IEndpointRouteBuilder MapBinaryFileTypeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var types = endpoints
            .MapGroup("/api/core/binary-file-types")
            .WithTags("Binary File Types")
            .RequireAuthorization();

        _ = types
            .MapGet("/", GetTypesAsync)
            .WithName("CoreGetBinaryFileTypes")
            .WithSummary("List configured binary file types")
            .Produces<IReadOnlyList<BinaryFileTypeResponse>>(StatusCodes.Status200OK);

        _ = types
            .MapPost("/", CreateTypeAsync)
            .WithName("CoreCreateBinaryFileType")
            .WithSummary("Create a binary file type policy")
            .Produces<BinaryFileTypeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = types
            .MapPut("/{id:guid}", UpdateTypeAsync)
            .WithName("CoreUpdateBinaryFileType")
            .WithSummary("Update a binary file type policy")
            .Produces<BinaryFileTypeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = types
            .MapDelete("/{id:guid}", DeleteTypeAsync)
            .WithName("CoreDeleteBinaryFileType")
            .WithSummary("Delete an unused non-system binary file type")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BinaryFileTypeResponse>>, ProblemHttpResult>
    > GetTypesAsync(
        string? search,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetBinaryFileTypesQuery,
            IReadOnlyList<BinaryFileTypeResponse>
        >(new GetBinaryFileTypesQuery(search), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<BinaryFileTypeResponse>, ProblemHttpResult>
    > CreateTypeAsync(
        CreateBinaryFileTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateBinaryFileTypeCommand,
            BinaryFileTypeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/binary-file-types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<BinaryFileTypeResponse>, ProblemHttpResult>
    > UpdateTypeAsync(
        Guid id,
        UpdateBinaryFileTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateBinaryFileTypeCommand,
            BinaryFileTypeResponse
        >(command with { BinaryFileTypeId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteTypeAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteBinaryFileTypeCommand, bool>(
            new DeleteBinaryFileTypeCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
