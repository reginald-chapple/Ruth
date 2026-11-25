using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Locations;

internal static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/locations")
            .WithTags("Locations")
            .RequireAuthorization();

        MapQueryRoutes(group);
        MapCommandRoutes(group);
        return endpoints;
    }

    private static void MapQueryRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/tree", GetTreeAsync)
            .WithName("CoreGetLocationTree")
            .WithSummary("Get hierarchical location tree")
            .Produces<IReadOnlyList<LocationTreeNodeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetLocationById")
            .WithSummary("Get location details by identifier")
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/within-radius", FindWithinRadiusAsync)
            .WithName("CoreFindLocationsWithinRadius")
            .WithSummary("Find locations within a spherical radius in meters using PostGIS")
            .Produces<IReadOnlyList<LocationDistanceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/containing-point", FindContainingPointAsync)
            .WithName("CoreFindLocationsContainingPoint")
            .WithSummary("Find locations whose PostGIS geofence polygon covers a point")
            .Produces<IReadOnlyList<LocationResponse>>(StatusCodes.Status200OK);
    }

    private static void MapCommandRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateLocation")
            .WithSummary("Create a named location, room, geofence, or street address")
            .Produces<LocationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateLocation")
            .WithSummary("Update an existing location")
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}/move", MoveAsync)
            .WithName("CoreMoveLocation")
            .WithSummary("Reparent a location in the location hierarchy with cycle protection")
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPost("/{id:guid}/verify", VerifyAsync)
            .WithName("CoreVerifyLocationAddress")
            .WithSummary("Run registered address verification and geocoding providers")
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteLocation")
            .WithSummary("Delete a leaf location")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<LocationTreeNodeResponse>>, ProblemHttpResult>
    > GetTreeAsync(
        Guid? rootLocationId,
        bool? namedOnly,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetLocationTreeQuery,
            IReadOnlyList<LocationTreeNodeResponse>
        >(
            new GetLocationTreeQuery(rootLocationId, namedOnly ?? true, activeOnly ?? false),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<LocationResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetLocationByIdQuery, LocationResponse>(
            new GetLocationByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<LocationDistanceResponse>>, ProblemHttpResult>
    > FindWithinRadiusAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            FindLocationsWithinRadiusQuery,
            IReadOnlyList<LocationDistanceResponse>
        >(
            new FindLocationsWithinRadiusQuery(
                latitude,
                longitude,
                radiusMeters,
                activeOnly ?? true
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<LocationResponse>>, ProblemHttpResult>
    > FindContainingPointAsync(
        double latitude,
        double longitude,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            FindLocationsContainingPointQuery,
            IReadOnlyList<LocationResponse>
        >(
            new FindLocationsContainingPointQuery(latitude, longitude, activeOnly ?? true),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<LocationResponse>, ProblemHttpResult>> CreateAsync(
        CreateLocationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateLocationCommand, LocationResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/locations/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<LocationResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateLocationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateLocationCommand, LocationResponse>(
            command with
            {
                LocationId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<LocationResponse>, ProblemHttpResult>> MoveAsync(
        Guid id,
        MoveLocationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<MoveLocationCommand, LocationResponse>(
            command with
            {
                LocationId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<LocationResponse>, ProblemHttpResult>> VerifyAsync(
        Guid id,
        bool? reVerify,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<VerifyLocationAddressCommand, LocationResponse>(
            new VerifyLocationAddressCommand(id, reVerify ?? false),
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
        var result = await dispatcher.SendAsync<DeleteLocationCommand, bool>(
            new DeleteLocationCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
