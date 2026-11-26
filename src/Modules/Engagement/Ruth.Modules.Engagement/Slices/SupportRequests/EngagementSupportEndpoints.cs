using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal static class EngagementSupportEndpoints
{
    public static IEndpointRouteBuilder MapEngagementSupportEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var staff = endpoints
            .MapGroup("/api/engagement/support")
            .WithTags("Engagement Care & Support Queue")
            .RequireAuthorization();

        _ = staff
            .MapGet("/categories", ListCategoriesAsync)
            .WithName("EngagementListSupportCategories")
            .WithSummary("List support categories")
            .WithDescription(
                "Returns organization-agnostic care, assistance, and wellbeing categories."
            )
            .Produces<IReadOnlyList<SupportCategoryResponse>>(StatusCodes.Status200OK);

        _ = staff
            .MapPost("/categories", CreateCategoryAsync)
            .WithName("EngagementCreateSupportCategory")
            .WithSummary("Create support category")
            .WithDescription(
                "Creates a care/support category with moderation and expiration settings."
            )
            .Produces<SupportCategoryResponse>(StatusCodes.Status201Created);

        _ = staff
            .MapGet("/queue", GetReviewQueueAsync)
            .WithName("EngagementGetCareTeamReviewQueue")
            .WithSummary("Get Care-Team Moderation & Guided Review Queue")
            .WithDescription("Returns prioritized pending, flagged, and active care requests.")
            .Produces<IReadOnlyList<SupportRequestResponse>>(StatusCodes.Status200OK);

        _ = staff
            .MapPost("/{id:guid}/moderate", ModerateAsync)
            .WithName("EngagementModerateSupportRequest")
            .WithSummary("Moderate support request")
            .WithDescription("Approves, edits, flags, or archives a support request.")
            .Produces<SupportRequestResponse>(StatusCodes.Status200OK);

        _ = staff
            .MapPost("/{id:guid}/resolve", ResolveAsync)
            .WithName("EngagementResolveSupportRequest")
            .WithSummary("Resolve support request")
            .WithDescription("Records an outcome summary and optionally archives the request.")
            .Produces<SupportRequestResponse>(StatusCodes.Status200OK);

        MapPublicSupportRoutes(endpoints);
        return endpoints;
    }

    private static void MapPublicSupportRoutes(IEndpointRouteBuilder endpoints)
    {
        var pub = endpoints
            .MapGroup("/api/engagement/public/support")
            .WithTags("Public Community Care & Support Wall")
            .AllowAnonymous();

        _ = pub.MapGet("/categories", ListPublicCategoriesAsync)
            .WithName("EngagementListPublicSupportCategories")
            .WithSummary("List active public support categories")
            .WithDescription("Returns active support categories for intake.")
            .Produces<IReadOnlyList<SupportCategoryResponse>>(StatusCodes.Status200OK);

        _ = pub.MapGet("/wall", GetPublicWallAsync)
            .WithName("EngagementGetPublicSupportWall")
            .WithSummary("Get public Community Care / Support Wall")
            .WithDescription(
                "Returns moderated, non-expired public support items with PII redacted."
            )
            .Produces<IReadOnlyList<SupportRequestResponse>>(StatusCodes.Status200OK);

        _ = pub.MapPost("/submit", SubmitPublicAsync)
            .WithName("EngagementSubmitSupportRequest")
            .WithSummary("Submit support request")
            .WithDescription(
                "Submits a community care or support request into the moderation/care queue."
            )
            .Produces<SupportRequestResponse>(StatusCodes.Status201Created);

        _ = pub.MapPost("/{id:guid}/support", SupportPublicAsync)
            .WithName("EngagementRecordCommunitySupport")
            .WithSummary("Increment community support counter")
            .WithDescription("Increments the CommunitySupportCount on a moderated public request.")
            .Produces<SupportRequestResponse>(StatusCodes.Status200OK);

        _ = pub.MapPost("/{id:guid}/comments", CommentPublicAsync)
            .WithName("EngagementAddPublicSupportComment")
            .WithSummary("Post encouragement comment")
            .WithDescription("Posts a threaded comment on an approved support request.")
            .Produces<SupportRequestResponse>(StatusCodes.Status200OK);

        _ = pub.MapPost("/{id:guid}/flag", FlagPublicAsync)
            .WithName("EngagementFlagSupportRequest")
            .WithSummary("Flag public support request for review")
            .WithDescription(
                "Flags a request and pulls it back into moderation once threshold is reached."
            )
            .Produces<SupportRequestResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SupportCategoryResponse>>, ProblemHttpResult>
    > ListCategoriesAsync(
        bool? activeOnly,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetSupportCategoriesQuery,
            IReadOnlyList<SupportCategoryResponse>
        >(new GetSupportCategoriesQuery(activeOnly ?? false), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SupportCategoryResponse>>, ProblemHttpResult>
    > ListPublicCategoriesAsync(IQueryDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.QueryAsync<
            GetSupportCategoriesQuery,
            IReadOnlyList<SupportCategoryResponse>
        >(new GetSupportCategoriesQuery(ActiveOnly: true), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<SupportCategoryResponse>, ProblemHttpResult>
    > CreateCategoryAsync(
        SupportCategorySpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateSupportCategoryCommand, SupportCategoryResponse>(
            new CreateSupportCategoryCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/support/categories/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SupportRequestResponse>>, ProblemHttpResult>
    > GetReviewQueueAsync(
        Guid? supportCategoryId,
        Guid? branchId,
        SupportModerationState? state,
        bool? includeArchived,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetCareTeamReviewQueueQuery,
            IReadOnlyList<SupportRequestResponse>
        >(
            new GetCareTeamReviewQueueQuery(
                supportCategoryId,
                branchId,
                state,
                includeArchived ?? false
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<SupportRequestResponse>, ProblemHttpResult>> ModerateAsync(
        Guid id,
        ModerateSupportRequestCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ModerateSupportRequestCommand, SupportRequestResponse>(
            body with
            {
                SupportRequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<Ok<SupportRequestResponse>, ProblemHttpResult>> ResolveAsync(
        Guid id,
        ResolveSupportRequestCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ResolveSupportRequestCommand, SupportRequestResponse>(
            body with
            {
                SupportRequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SupportRequestResponse>>, ProblemHttpResult>
    > GetPublicWallAsync(
        Guid? supportCategoryId,
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetPublicSupportWallQuery,
            IReadOnlyList<SupportRequestResponse>
        >(new GetPublicSupportWallQuery(supportCategoryId, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<SupportRequestResponse>, ProblemHttpResult>
    > SubmitPublicAsync(
        SubmitSupportRequestCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<SubmitSupportRequestCommand, SupportRequestResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/public/support/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<SupportRequestResponse>, ProblemHttpResult>
    > SupportPublicAsync(Guid id, ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<RecordCommunitySupportCommand, SupportRequestResponse>(
            new RecordCommunitySupportCommand(id, IsCareTeamMember: false),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<SupportRequestResponse>, ProblemHttpResult>
    > CommentPublicAsync(
        Guid id,
        AddSupportRequestCommentCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<AddSupportRequestCommentCommand, SupportRequestResponse>(
            body with
            {
                SupportRequestId = id,
                IsInternalCareNote = false,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<SupportRequestResponse>, ProblemHttpResult>
    > FlagPublicAsync(Guid id, ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<FlagSupportRequestCommand, SupportRequestResponse>(
            new FlagSupportRequestCommand(id),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
