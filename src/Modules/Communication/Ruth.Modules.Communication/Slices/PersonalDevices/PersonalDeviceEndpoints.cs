using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal static class PersonalDeviceEndpoints
{
    public static IEndpointRouteBuilder MapPersonalDeviceEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/personal-devices")
            .WithTags("Communication Personal Devices")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListPersonalDevices")
            .WithSummary("List registered personal push notification devices")
            .Produces<IReadOnlyList<PersonalDeviceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", RegisterAsync)
            .WithName("CommunicationRegisterPersonalDevice")
            .WithSummary("Register or refresh a personal device push notification token")
            .Produces<PersonalDeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}/notifications", SetNotificationsAsync)
            .WithName("CommunicationSetPersonalDeviceNotifications")
            .WithSummary("Update push notification opt-in preferences on a personal device")
            .Produces<PersonalDeviceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CommunicationDeletePersonalDevice")
            .WithSummary("Delete a personal device push registration")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonalDeviceResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? personId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonalDevicesQuery,
            IReadOnlyList<PersonalDeviceResponse>
        >(new GetPersonalDevicesQuery(personId, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<PersonalDeviceResponse>, ProblemHttpResult>> RegisterAsync(
        RegisterPersonalDeviceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RegisterPersonalDeviceCommand,
            PersonalDeviceResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonalDeviceResponse>, ProblemHttpResult>
    > SetNotificationsAsync(
        Guid id,
        SetPersonalDeviceNotificationsCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetPersonalDeviceNotificationsCommand,
            PersonalDeviceResponse
        >(command with { PersonalDeviceId = id }, cancellationToken);
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
        var result = await dispatcher.SendAsync<DeletePersonalDeviceCommand, bool>(
            new DeletePersonalDeviceCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
