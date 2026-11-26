using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal static class SystemCommunicationEndpoints
{
    public static IEndpointRouteBuilder MapSystemCommunicationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/system-communications")
            .WithTags("Communication System Notifications")
            .RequireAuthorization();

        RegisterCrudRoutes(group);
        RegisterDispatchRoutes(group);
        return endpoints;
    }

    private static void RegisterCrudRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListSystemCommunications")
            .WithSummary("List transactional system communication templates")
            .Produces<IReadOnlyList<SystemCommunicationResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CommunicationGetSystemCommunicationById")
            .WithSummary("Get a system communication template by identifier")
            .Produces<SystemCommunicationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CommunicationCreateSystemCommunication")
            .WithSummary("Create a custom system communication template")
            .Produces<SystemCommunicationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CommunicationUpdateSystemCommunication")
            .WithSummary("Update an existing system communication template")
            .Produces<SystemCommunicationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CommunicationDeleteSystemCommunication")
            .WithSummary("Delete a non-system communication template")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void RegisterDispatchRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/send", SendAsync)
            .WithName("CommunicationSendSystemCommunication")
            .WithSummary("Render and dispatch a system communication across Email, SMS, and Push")
            .Produces<SystemCommunicationSendResultResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/logs", ListLogsAsync)
            .WithName("CommunicationListSystemNotificationLogs")
            .WithSummary("List recent transactional system notification delivery audit logs")
            .Produces<IReadOnlyList<SystemNotificationLogResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SystemCommunicationResponse>>, ProblemHttpResult>
    > ListAsync(
        SystemCommunicationCategory? category,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSystemCommunicationsQuery,
            IReadOnlyList<SystemCommunicationResponse>
        >(new GetSystemCommunicationsQuery(category, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SystemCommunicationResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSystemCommunicationByIdQuery,
            SystemCommunicationResponse
        >(new GetSystemCommunicationByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SystemCommunicationResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateSystemCommunicationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateSystemCommunicationCommand,
            SystemCommunicationResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/system-communications/{result.Value.Id:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SystemCommunicationResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateSystemCommunicationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateSystemCommunicationCommand,
            SystemCommunicationResponse
        >(command with { SystemCommunicationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteSystemCommunicationCommand, bool>(
            new DeleteSystemCommunicationCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SystemCommunicationSendResultResponse>, ProblemHttpResult>
    > SendAsync(
        SendSystemCommunicationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SystemNotificationLogResponse>>, ProblemHttpResult>
    > ListLogsAsync(
        string? systemCommunicationKey,
        CommunicationChannel? channel,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSystemNotificationLogsQuery,
            IReadOnlyList<SystemNotificationLogResponse>
        >(
            new GetSystemNotificationLogsQuery(systemCommunicationKey, channel, limit ?? 50),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
