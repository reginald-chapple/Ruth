using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Devices;

internal static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/core/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        _ = group
            .MapGet("/", GetAllAsync)
            .WithName("CoreGetDevices")
            .WithSummary("List physical devices (kiosks, label printers, tablets)")
            .Produces<IReadOnlyList<DeviceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CoreGetDeviceById")
            .WithSummary("Get physical device details")
            .Produces<DeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/match", MatchAsync)
            .WithName("CoreMatchDevice")
            .WithSummary("Match an active device by IP address, hostname, or PostGIS geofence")
            .Produces<DeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CoreCreateDevice")
            .WithSummary("Register a physical device")
            .Produces<DeviceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CoreUpdateDevice")
            .WithSummary("Update an existing physical device")
            .Produces<DeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CoreDeleteDevice")
            .WithSummary("Delete a physical device")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<DeviceResponse>>, ProblemHttpResult>
    > GetAllAsync(
        Guid? deviceTypeValueId,
        bool? activeOnly,
        string? search,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetDevicesQuery, IReadOnlyList<DeviceResponse>>(
            new GetDevicesQuery(deviceTypeValueId, activeOnly ?? false, search),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<DeviceResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetDeviceByIdQuery, DeviceResponse>(
            new GetDeviceByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<DeviceResponse>, ProblemHttpResult>> MatchAsync(
        Guid deviceTypeValueId,
        string? ipAddress,
        double? latitude,
        double? longitude,
        bool? skipReverseDns,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<MatchDeviceQuery, DeviceResponse>(
            new MatchDeviceQuery(
                deviceTypeValueId,
                ipAddress,
                latitude,
                longitude,
                skipReverseDns ?? true
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<DeviceResponse>, ProblemHttpResult>> CreateAsync(
        CreateDeviceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateDeviceCommand, DeviceResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/devices/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<DeviceResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateDeviceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateDeviceCommand, DeviceResponse>(
            command with
            {
                DeviceId = id,
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
        var result = await dispatcher.SendAsync<DeleteDeviceCommand, bool>(
            new DeleteDeviceCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
