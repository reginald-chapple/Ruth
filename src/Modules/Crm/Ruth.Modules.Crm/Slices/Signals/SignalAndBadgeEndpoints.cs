using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Signals;

internal static class SignalAndBadgeEndpoints
{
    public static IEndpointRouteBuilder MapSignalAndBadgeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        MapSignalRoutes(endpoints);
        MapBadgeRoutes(endpoints);
        return endpoints;
    }

    private static void MapSignalRoutes(IEndpointRouteBuilder endpoints)
    {
        var signalGroup = endpoints
            .MapGroup("/api/crm/signals")
            .WithTags("CRM Signals")
            .RequireAuthorization();

        _ = signalGroup
            .MapGet("/types", GetSignalTypesAsync)
            .WithName("CrmGetSignalTypes")
            .WithSummary("List configured safety/VIP signal types")
            .Produces<IReadOnlyList<SignalTypeResponse>>(StatusCodes.Status200OK);

        _ = signalGroup
            .MapPost("/types", CreateSignalTypeAsync)
            .WithName("CrmCreateSignalType")
            .WithSummary("Create a new signal type")
            .Produces<SignalTypeResponse>(StatusCodes.Status201Created);

        _ = signalGroup
            .MapPut("/types/{id:guid}", UpdateSignalTypeAsync)
            .WithName("CrmUpdateSignalType")
            .WithSummary("Update a signal type and recalculate TopSignal on affected persons")
            .Produces<SignalTypeResponse>(StatusCodes.Status200OK);

        _ = signalGroup
            .MapGet("/person/{personId:guid}", GetPersonSignalsAsync)
            .WithName("CrmGetPersonSignals")
            .WithSummary("List signals attached to a person")
            .Produces<IReadOnlyList<PersonSignalResponse>>(StatusCodes.Status200OK);

        _ = signalGroup
            .MapPost("/person", AddPersonSignalAsync)
            .WithName("CrmAddPersonSignal")
            .WithSummary("Attach a signal to a person and recalculate TopSignal")
            .Produces<PersonSignalResponse>(StatusCodes.Status201Created);

        _ = signalGroup
            .MapDelete("/person/{id:guid}", RemovePersonSignalAsync)
            .WithName("CrmRemovePersonSignal")
            .WithSummary("Remove a signal from a person and recalculate TopSignal")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapBadgeRoutes(IEndpointRouteBuilder endpoints)
    {
        var badgeGroup = endpoints
            .MapGroup("/api/crm/badges")
            .WithTags("CRM Badges")
            .RequireAuthorization();

        _ = badgeGroup
            .MapGet("/definitions", GetBadgeDefinitionsAsync)
            .WithName("CrmGetBadgeDefinitions")
            .WithSummary("List configured badge definitions")
            .Produces<IReadOnlyList<BadgeDefinitionResponse>>(StatusCodes.Status200OK);

        _ = badgeGroup
            .MapPost("/definitions", CreateBadgeDefinitionAsync)
            .WithName("CrmCreateBadgeDefinition")
            .WithSummary("Create a badge definition")
            .Produces<BadgeDefinitionResponse>(StatusCodes.Status201Created);

        _ = badgeGroup
            .MapGet("/person/{personId:guid}", EvaluatePersonBadgesAsync)
            .WithName("CrmEvaluatePersonBadges")
            .WithSummary("Evaluate all active modular badges for a person")
            .Produces<IReadOnlyList<EvaluatedPersonBadgeResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SignalTypeResponse>>, ProblemHttpResult>
    > GetSignalTypesAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSignalTypesQuery,
            IReadOnlyList<SignalTypeResponse>
        >(new GetSignalTypesQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SignalTypeResponse>, ProblemHttpResult>
    > CreateSignalTypeAsync(
        CreateSignalTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateSignalTypeCommand, SignalTypeResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/signals/types/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SignalTypeResponse>, ProblemHttpResult>
    > UpdateSignalTypeAsync(
        Guid id,
        UpdateSignalTypeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateSignalTypeCommand, SignalTypeResponse>(
            command with
            {
                SignalTypeId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonSignalResponse>>, ProblemHttpResult>
    > GetPersonSignalsAsync(
        Guid personId,
        bool? includeExpired,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonSignalsQuery,
            IReadOnlyList<PersonSignalResponse>
        >(new GetPersonSignalsQuery(personId, includeExpired ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<PersonSignalResponse>, ProblemHttpResult>
    > AddPersonSignalAsync(
        AddPersonSignalCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddPersonSignalCommand, PersonSignalResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/signals/person/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemovePersonSignalAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemovePersonSignalCommand, bool>(
            new RemovePersonSignalCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BadgeDefinitionResponse>>, ProblemHttpResult>
    > GetBadgeDefinitionsAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetBadgeDefinitionsQuery,
            IReadOnlyList<BadgeDefinitionResponse>
        >(new GetBadgeDefinitionsQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<BadgeDefinitionResponse>, ProblemHttpResult>
    > CreateBadgeDefinitionAsync(
        CreateBadgeDefinitionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateBadgeDefinitionCommand,
            BadgeDefinitionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/badges/definitions/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EvaluatedPersonBadgeResponse>>, ProblemHttpResult>
    > EvaluatePersonBadgesAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetEvaluatedPersonBadgesQuery,
            IReadOnlyList<EvaluatedPersonBadgeResponse>
        >(new GetEvaluatedPersonBadgesQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
