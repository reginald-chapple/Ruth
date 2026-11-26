using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal static class EngagementPipelinePublicEndpoints
{
    public static IEndpointRouteBuilder MapEngagementPipelinePublicEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var pub = endpoints
            .MapGroup("/api/engagement/public")
            .WithTags("Public Opportunity Finder & Signup")
            .AllowAnonymous();

        _ = pub
            .MapGet("/opportunities", ListPublicOpportunitiesAsync)
            .WithName("EngagementListPublicOpportunities")
            .WithSummary("Search public engagement opportunities")
            .WithDescription("Returns active public opportunities for volunteer, onboarding, and mentorship intake.")
            .Produces<IReadOnlyList<EngagementOpportunityResponse>>(StatusCodes.Status200OK);

        _ = pub
            .MapGet("/opportunities/{idOrSlug}", GetPublicOpportunityAsync)
            .WithName("EngagementGetPublicOpportunity")
            .WithSummary("Get public engagement opportunity by ID or slug")
            .WithDescription("Returns details for a single public engagement opportunity.")
            .Produces<EngagementOpportunityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = pub
            .MapPost("/signup", SubmitPublicSignupAsync)
            .WithName("EngagementSubmitPublicSignup")
            .WithSummary("Submit public opportunity signup")
            .WithDescription("Resolves or creates a CRM constituent and places an EngagementRequest on the Connector Board.")
            .Produces<EngagementRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EngagementOpportunityResponse>>, ProblemHttpResult>
    > ListPublicOpportunitiesAsync(
        Guid? pipelineTypeId,
        Guid? branchId,
        string? q,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetEngagementOpportunitiesQuery,
            IReadOnlyList<EngagementOpportunityResponse>
        >(
            new GetEngagementOpportunitiesQuery(
                pipelineTypeId,
                branchId,
                ActiveOnly: true,
                PublicOnly: true,
                SearchText: q
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementOpportunityResponse>, ProblemHttpResult>
    > GetPublicOpportunityAsync(
        string idOrSlug,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetEngagementOpportunityByIdOrSlugQuery,
            EngagementOpportunityResponse
        >(new GetEngagementOpportunityByIdOrSlugQuery(idOrSlug), ct);
        if (r.IsFailure)
        {
            return EngagementProblemResultMapper.ToProblemResult(r, ctx);
        }

        return r.Value is { IsActive: true, IsPublic: true }
            ? TypedResults.Ok(r.Value)
            : TypedResults.Problem(
                detail: "Public opportunity not found.",
                instance: ctx.Request.Path.Value,
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found"
            );
    }

    private static async Task<
        Results<Created<EngagementRequestResponse>, ProblemHttpResult>
    > SubmitPublicSignupAsync(
        SubmitPublicEngagementSignupCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<SubmitPublicEngagementSignupCommand, EngagementRequestResponse>(
            body,
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/pipelines/requests/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
