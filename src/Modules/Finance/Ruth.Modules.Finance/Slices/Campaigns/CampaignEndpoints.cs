using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal static class CampaignEndpoints
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var staffGroup = endpoints
            .MapGroup("/api/finance/campaigns")
            .WithTags("Finance Fundraising Campaigns")
            .RequireAuthorization();

        MapCampaignAdminRoutes(staffGroup);
        MapParticipantAndLeaderRoutes(staffGroup);

        var publicGroup = endpoints
            .MapGroup("/api/finance/campaigns/public")
            .WithTags("Finance Fundraising Campaigns")
            .AllowAnonymous();

        MapPublicPortalRoutes(publicGroup);
        return endpoints;
    }

    private static void MapCampaignAdminRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListCampaignsAsync)
            .WithName("FinanceListFundraisingCampaigns")
            .WithSummary("List peer-to-peer, team, and project fundraising campaigns")
            .WithDescription(
                "Returns fundraising campaigns configured on Groups and Financial Accounts."
            )
            .Produces<IReadOnlyList<FundraisingCampaignResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateCampaignAsync)
            .WithName("FinanceCreateFundraisingCampaign")
            .WithSummary("Create a fundraising campaign")
            .WithDescription(
                "Configures a campaign linking a Group and Financial Account with team/participant goal policies."
            )
            .Produces<FundraisingCampaignResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateCampaignAsync)
            .WithName("FinanceUpdateFundraisingCampaign")
            .WithSummary("Update a fundraising campaign")
            .WithDescription("Updates campaign goals, dates, or portal configuration.")
            .Produces<FundraisingCampaignResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}/team-goal", SetTeamGoalOverrideAsync)
            .WithName("FinanceSetCampaignTeamGoalOverride")
            .WithSummary("Set or clear a team fundraising goal override")
            .WithDescription("Updates the team goal override when permitted by campaign policy.")
            .Produces<FundraisingCampaignResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapParticipantAndLeaderRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/{id:guid}/toolbox", GetLeaderToolboxAsync)
            .WithName("FinanceGetCampaignLeaderToolbox")
            .WithSummary("Get Campaign Leader Toolbox roster and contribution credits")
            .WithDescription(
                "Returns full participant goal overrides, progress percentages, and contribution credit ledger."
            )
            .Produces<CampaignLeaderToolboxResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/participants", EnrollParticipantAsync)
            .WithName("FinanceEnrollCampaignParticipant")
            .WithSummary("Enroll a participant fundraiser in a campaign")
            .WithDescription(
                "Adds a constituent to a fundraising campaign with optional custom goal."
            )
            .Produces<CampaignParticipantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}/participants/{participantId:guid}", UpdateParticipantGoalAsync)
            .WithName("FinanceUpdateCampaignParticipantGoal")
            .WithSummary("Update a participant's personal fundraising goal override")
            .WithDescription("Updates personal goal override, message, and public visibility.")
            .Produces<CampaignParticipantResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/sync-group-members", SyncGroupMembersAsync)
            .WithName("FinanceSyncCampaignGroupMembers")
            .WithSummary("Sync active Group members into campaign participants")
            .WithDescription("Enrolls active members of the linked Group into the campaign.")
            .Produces<IReadOnlyList<CampaignParticipantResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapPublicPortalRoutes(RouteGroupBuilder publicGroup)
    {
        _ = publicGroup
            .MapGet("/{slug}", GetPublicPortalAsync)
            .WithName("FinanceGetPublicCampaignPortal")
            .WithSummary("Get public fundraising campaign portal by slug")
            .WithDescription(
                "Returns campaign progress, public participant profiles, and privacy-filtered donor feed."
            )
            .Produces<PublicCampaignPortalResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = publicGroup
            .MapPost("/{slug}/contributions", SubmitPublicContributionAsync)
            .WithName("FinanceSubmitCampaignContribution")
            .WithSummary("Submit a public contribution with donor privacy and participant credit")
            .WithDescription(
                "Records a contribution transaction to the campaign's Financial Account and credits the campaign/participant."
            )
            .Produces<CampaignContributionReceiptResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FundraisingCampaignResponse>>, ProblemHttpResult>
    > ListCampaignsAsync(
        Guid? groupId,
        Guid? financialAccountId,
        FundraisingCampaignStatus? status,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetFundraisingCampaignsQuery,
            IReadOnlyList<FundraisingCampaignResponse>
        >(new GetFundraisingCampaignsQuery(groupId, financialAccountId, status), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<FundraisingCampaignResponse>, ProblemHttpResult>
    > CreateCampaignAsync(
        FundraisingCampaignSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateFundraisingCampaignCommand,
            FundraisingCampaignResponse
        >(new CreateFundraisingCampaignCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/finance/campaigns/{res.Value.Id:D}", res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<FundraisingCampaignResponse>, ProblemHttpResult>
    > UpdateCampaignAsync(
        Guid id,
        FundraisingCampaignSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateFundraisingCampaignCommand,
            FundraisingCampaignResponse
        >(new UpdateFundraisingCampaignCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<FundraisingCampaignResponse>, ProblemHttpResult>
    > SetTeamGoalOverrideAsync(
        Guid id,
        SetCampaignTeamGoalOverrideCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            SetCampaignTeamGoalOverrideCommand,
            FundraisingCampaignResponse
        >(body with { CampaignId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<CampaignLeaderToolboxResponse>, ProblemHttpResult>
    > GetLeaderToolboxAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetCampaignLeaderToolboxQuery,
            CampaignLeaderToolboxResponse
        >(new GetCampaignLeaderToolboxQuery(id), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<CampaignParticipantResponse>, ProblemHttpResult>
    > EnrollParticipantAsync(
        Guid id,
        EnrollCampaignParticipantCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            EnrollCampaignParticipantCommand,
            CampaignParticipantResponse
        >(body with { CampaignId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/campaigns/{id:D}/participants/{res.Value.Id:D}",
                res.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<CampaignParticipantResponse>, ProblemHttpResult>
    > UpdateParticipantGoalAsync(
        Guid id,
        Guid participantId,
        UpdateCampaignParticipantGoalCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateCampaignParticipantGoalCommand,
            CampaignParticipantResponse
        >(body with { CampaignId = id, ParticipantId = participantId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CampaignParticipantResponse>>, ProblemHttpResult>
    > SyncGroupMembersAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            SyncGroupMembersToCampaignCommand,
            IReadOnlyList<CampaignParticipantResponse>
        >(new SyncGroupMembersToCampaignCommand(id), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<PublicCampaignPortalResponse>, ProblemHttpResult>
    > GetPublicPortalAsync(
        string slug,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetPublicCampaignPortalQuery,
            PublicCampaignPortalResponse
        >(new GetPublicCampaignPortalQuery(slug), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<CampaignContributionReceiptResponse>, ProblemHttpResult>
    > SubmitPublicContributionAsync(
        string slug,
        SubmitCampaignContributionCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            SubmitCampaignContributionCommand,
            CampaignContributionReceiptResponse
        >(body with { CampaignIdOrSlug = slug }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/finance/campaigns/public/{slug}", res.Value)
            : FinanceProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
