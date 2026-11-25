using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.Households;

internal static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/crm/households")
            .WithTags("CRM Households")
            .RequireAuthorization();

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CrmGetHouseholdById")
            .WithSummary("Get household details, salutations, giving leader, and locations")
            .Produces<HouseholdResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/by-person/{personId:guid}", GetByPersonAsync)
            .WithName("CrmGetPersonHouseholds")
            .WithSummary("List all households a person belongs to (including blended households)")
            .Produces<IReadOnlyList<HouseholdResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CrmCreateHousehold")
            .WithSummary("Create a new household")
            .Produces<HouseholdResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CrmUpdateHousehold")
            .WithSummary("Update household metadata")
            .Produces<HouseholdResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/members", AddMemberAsync)
            .WithName("CrmAddHouseholdMember")
            .WithSummary("Add or update a person membership in a household")
            .Produces<HouseholdResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}/members/{personId:guid}", RemoveMemberAsync)
            .WithName("CrmRemoveHouseholdMember")
            .WithSummary("Remove a person from a household")
            .Produces<HouseholdResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/locations", SetLocationAsync)
            .WithName("CrmSetHouseholdLocation")
            .WithSummary("Attach a physical Core Location to a household")
            .Produces<HouseholdResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<Results<Ok<HouseholdResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetHouseholdByIdQuery, HouseholdResponse>(
            new GetHouseholdByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<HouseholdResponse>>, ProblemHttpResult>
    > GetByPersonAsync(
        Guid personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonHouseholdsQuery,
            IReadOnlyList<HouseholdResponse>
        >(new GetPersonHouseholdsQuery(personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<HouseholdResponse>, ProblemHttpResult>> CreateAsync(
        CreateHouseholdCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/households/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<HouseholdResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateHouseholdCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateHouseholdCommand, HouseholdResponse>(
            command with
            {
                HouseholdId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<HouseholdResponse>, ProblemHttpResult>> AddMemberAsync(
        Guid id,
        AddHouseholdMemberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddHouseholdMemberCommand, HouseholdResponse>(
            command with
            {
                HouseholdId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<HouseholdResponse>, ProblemHttpResult>> RemoveMemberAsync(
        Guid id,
        Guid personId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveHouseholdMemberCommand, HouseholdResponse>(
            new RemoveHouseholdMemberCommand(id, personId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<HouseholdResponse>, ProblemHttpResult>> SetLocationAsync(
        Guid id,
        SetHouseholdLocationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<SetHouseholdLocationCommand, HouseholdResponse>(
            command with
            {
                HouseholdId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
