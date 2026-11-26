using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal static class SystemPhoneNumberEndpoints
{
    public static IEndpointRouteBuilder MapSystemPhoneNumberEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/phone-numbers")
            .WithTags("Communication Phone Numbers")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListSystemPhoneNumbers")
            .WithSummary("List organization-owned E.164 system phone numbers")
            .Produces<IReadOnlyList<SystemPhoneNumberResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CommunicationCreateSystemPhoneNumber")
            .WithSummary("Register a system phone number")
            .Produces<SystemPhoneNumberResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CommunicationUpdateSystemPhoneNumber")
            .WithSummary("Update an existing system phone number")
            .Produces<SystemPhoneNumberResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CommunicationDeleteSystemPhoneNumber")
            .WithSummary("Delete an unused system phone number")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SystemPhoneNumberResponse>>, ProblemHttpResult>
    > ListAsync(
        bool? activeOnly,
        bool? smsEnabledOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSystemPhoneNumbersQuery,
            IReadOnlyList<SystemPhoneNumberResponse>
        >(
            new GetSystemPhoneNumbersQuery(activeOnly ?? false, smsEnabledOnly ?? false),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SystemPhoneNumberResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateSystemPhoneNumberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateSystemPhoneNumberCommand,
            SystemPhoneNumberResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/phone-numbers/{result.Value.Id:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SystemPhoneNumberResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateSystemPhoneNumberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateSystemPhoneNumberCommand,
            SystemPhoneNumberResponse
        >(command with { SystemPhoneNumberId = id }, cancellationToken);
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
        var result = await dispatcher.SendAsync<DeleteSystemPhoneNumberCommand, bool>(
            new DeleteSystemPhoneNumberCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
