using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.EntityTypes;

internal static class EntityTypeEndpoints
{
    public static IEndpointRouteBuilder MapEntityTypeAndFieldTypeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core")
            .WithTags("Core Registry")
            .RequireAuthorization();

        _ = group
            .MapGet("/entity-types", GetEntityTypesAsync)
            .WithName("CoreGetEntityTypes")
            .WithSummary("List compile-time discovered modular entity types")
            .Produces<IReadOnlyList<EntityTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/entity-types/{key}", GetEntityTypeByKeyAsync)
            .WithName("CoreGetEntityTypeByKey")
            .WithSummary("Get a registered entity type by canonical key")
            .Produces<EntityTypeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/field-types", GetFieldTypesAsync)
            .WithName("CoreGetFieldTypes")
            .WithSummary("List all registered modular attribute FieldType plugins")
            .Produces<IReadOnlyList<FieldTypeResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EntityTypeResponse>>, ProblemHttpResult>
    > GetEntityTypesAsync(
        bool? onlySecured,
        bool? onlySupportingAttributes,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetEntityTypesQuery,
            IReadOnlyList<EntityTypeResponse>
        >(new GetEntityTypesQuery(onlySecured, onlySupportingAttributes), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<EntityTypeResponse>, ProblemHttpResult>
    > GetEntityTypeByKeyAsync(
        string key,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
            new GetEntityTypeByKeyQuery(key),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FieldTypeResponse>>, ProblemHttpResult>
    > GetFieldTypesAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFieldTypesQuery,
            IReadOnlyList<FieldTypeResponse>
        >(new GetFieldTypesQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
