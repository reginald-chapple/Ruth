using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Organizations;

internal static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/organizations")
            .WithTags("Finance Organizations & Sponsors")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListOrganizations")
            .WithSummary("List organizations and corporate sponsors")
            .WithDescription(
                "Searches and lists CRM Business organizations and institutional sponsors."
            )
            .Produces<IReadOnlyList<OrganizationSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("FinanceGetOrganizationById")
            .WithSummary("Get organization or sponsor profile by ID")
            .WithDescription(
                "Returns organization metadata along with active pledged, open invoice, and settled ledger totals."
            )
            .Produces<OrganizationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateOrganization")
            .WithSummary("Create an organization or corporate sponsor")
            .WithDescription("Creates a new Business organization record via the CRM facade.")
            .Produces<OrganizationDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("FinanceUpdateOrganization")
            .WithSummary("Update an organization or corporate sponsor")
            .WithDescription("Updates organization details and sponsor/tax attributes.")
            .Produces<OrganizationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<OrganizationSummaryResponse>>, ProblemHttpResult>
    > ListAsync(
        string? searchTerm,
        Guid? branchId,
        int? maxResults,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetOrganizationsQuery,
            IReadOnlyList<OrganizationSummaryResponse>
        >(new GetOrganizationsQuery(searchTerm, branchId, maxResults ?? 50), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<OrganizationDetailResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetOrganizationByIdQuery,
            OrganizationDetailResponse
        >(new GetOrganizationByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<OrganizationDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateOrganizationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateOrganizationCommand,
            OrganizationDetailResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/organizations/{result.Value.OrganizationPersonId:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<OrganizationDetailResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateOrganizationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateOrganizationCommand,
            OrganizationDetailResponse
        >(command with { OrganizationPersonId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
