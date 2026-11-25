using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Slices.People;

internal static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/crm/people")
            .WithTags("CRM People")
            .RequireAuthorization();

        MapCorePersonRoutes(group);
        MapPersonChildRoutes(group);
        return endpoints;
    }

    private static void MapCorePersonRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/search", SearchAsync)
            .WithName("CrmSearchPeople")
            .WithSummary("Fast pg_trgm, phonetic, nickname, phone, and search-key person search")
            .Produces<IReadOnlyList<PersonSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CrmGetPersonById")
            .WithSummary("Get full Person 360 profile (automatically following merge redirects)")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CrmCreatePerson")
            .WithSummary("Create a new person record")
            .Produces<PersonDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CrmUpdatePerson")
            .WithSummary("Update an existing person record")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/meta/name-insights", GetNameInsightsAsync)
            .WithName("CrmGetMetaNameInsights")
            .WithSummary(
                "Inspect statistical gender prediction, nickname expansions, and Double Metaphone keys"
            )
            .Produces<MetaNameInsightsResponse>(StatusCodes.Status200OK);
    }

    private static void MapPersonChildRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/phones", UpsertPhoneAsync)
            .WithName("CrmUpsertPersonPhone")
            .WithSummary("Add or update a normalized E.164 phone number on a person")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}/phones/{phoneId:guid}", RemovePhoneAsync)
            .WithName("CrmRemovePersonPhone")
            .WithSummary("Remove a phone number from a person")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/previous-names", AddPreviousNameAsync)
            .WithName("CrmAddPersonPreviousName")
            .WithSummary("Record a previous last name for a person")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{id:guid}/search-keys", AddSearchKeyAsync)
            .WithName("CrmAddPersonSearchKey")
            .WithSummary("Add an alternate search key (barcode, email, legacy ID)")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{id:guid}/search-keys/{keyId:guid}", RemoveSearchKeyAsync)
            .WithName("CrmRemovePersonSearchKey")
            .WithSummary("Remove an alternate search key")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/{id:guid}/attributes", UpdateAttributesAsync)
            .WithName("CrmUpdatePersonAttributes")
            .WithSummary("Update JSONB extensible attributes on a person")
            .Produces<PersonDetailResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonSummaryResponse>>, ProblemHttpResult>
    > SearchAsync(
        string? query,
        PersonRecordStatus? recordStatus,
        PersonConnectionStatus? connectionStatus,
        Guid? primaryBranchId,
        bool? includeDeceased,
        bool? includeBusinesses,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(
            new SearchPeopleQuery(
                query,
                recordStatus,
                connectionStatus,
                primaryBranchId,
                includeDeceased ?? false,
                includeBusinesses ?? false,
                maxResults ?? 25
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<PersonDetailResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        bool? followRedirects,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(id, followRedirects ?? true),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<PersonDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreatePersonCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/crm/people/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<PersonDetailResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdatePersonCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdatePersonCommand, PersonDetailResponse>(
            command with
            {
                PersonId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > UpsertPhoneAsync(
        Guid id,
        UpsertPhoneNumberCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpsertPhoneNumberCommand, PersonDetailResponse>(
            command with
            {
                PersonId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > RemovePhoneAsync(
        Guid id,
        Guid phoneId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemovePhoneNumberCommand, PersonDetailResponse>(
            new RemovePhoneNumberCommand(id, phoneId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > AddPreviousNameAsync(
        Guid id,
        AddPersonPreviousNameCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddPersonPreviousNameCommand, PersonDetailResponse>(
            command with
            {
                PersonId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > AddSearchKeyAsync(
        Guid id,
        AddPersonSearchKeyCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AddPersonSearchKeyCommand, PersonDetailResponse>(
            command with
            {
                PersonId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > RemoveSearchKeyAsync(
        Guid id,
        Guid keyId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemovePersonSearchKeyCommand, PersonDetailResponse>(
            new RemovePersonSearchKeyCommand(id, keyId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonDetailResponse>, ProblemHttpResult>
    > UpdateAttributesAsync(
        Guid id,
        UpdatePersonAttributesCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdatePersonAttributesCommand,
            PersonDetailResponse
        >(command with { PersonId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<MetaNameInsightsResponse>, ProblemHttpResult>
    > GetNameInsightsAsync(
        string firstName,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetMetaNameInsightsQuery,
            MetaNameInsightsResponse
        >(new GetMetaNameInsightsQuery(firstName), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CrmProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
