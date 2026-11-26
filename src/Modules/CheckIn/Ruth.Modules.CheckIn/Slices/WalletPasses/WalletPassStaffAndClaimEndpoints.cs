using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal static class WalletPassStaffAndClaimEndpoints
{
    public static IEndpointRouteBuilder MapWalletPassStaffAndClaimEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var staff = endpoints
            .MapGroup("/api/checkin/wallet-passes")
            .WithTags("CheckIn - Wallet Passes")
            .RequireAuthorization();

        MapTemplateGetAndCreateRoutes(staff);
        MapTemplateUpdateAndDeleteRoutes(staff);
        MapIssuanceListAndIssueRoutes(staff);
        MapDynamicRevokePreviewAndScanRoutes(staff);
        MapAnonymousClaimRoutes(endpoints);

        return endpoints;
    }

    private static void MapTemplateGetAndCreateRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/templates",
                static async Task<
                    Results<Ok<IReadOnlyList<WalletPassTemplateResponse>>, ProblemHttpResult>
                > (
                    [FromQuery] bool? activeOnly,
                    IQueryHandler<
                        GetWalletPassTemplatesQuery,
                        IReadOnlyList<WalletPassTemplateResponse>
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(
                        new GetWalletPassTemplatesQuery(activeOnly ?? false),
                        ct
                    );
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("GetWalletPassTemplates");

        _ = group
            .MapPost(
                "/templates",
                static async Task<Results<Created<WalletPassTemplateResponse>, ProblemHttpResult>> (
                    CreateWalletPassTemplateCommand command,
                    ICommandHandler<
                        CreateWalletPassTemplateCommand,
                        WalletPassTemplateResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(command, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Created(
                            $"/api/checkin/wallet-passes/templates/{res.Value.Id:D}",
                            res.Value
                        );
                }
            )
            .WithName("CreateWalletPassTemplate");
    }

    private static void MapTemplateUpdateAndDeleteRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPut(
                "/templates/{id:guid}",
                static async Task<Results<Ok<WalletPassTemplateResponse>, ProblemHttpResult>> (
                    Guid id,
                    UpdateWalletPassTemplateCommand body,
                    ICommandHandler<
                        UpdateWalletPassTemplateCommand,
                        WalletPassTemplateResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(body with { TemplateId = id }, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("UpdateWalletPassTemplate");

        _ = group
            .MapDelete(
                "/templates/{id:guid}",
                static async Task<Results<NoContent, ProblemHttpResult>> (
                    Guid id,
                    ICommandHandler<DeleteWalletPassTemplateCommand> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(
                        new DeleteWalletPassTemplateCommand(id),
                        ct
                    );
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.NoContent();
                }
            )
            .WithName("DeleteWalletPassTemplate");
    }

    private static void MapIssuanceListAndIssueRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/",
                static async Task<
                    Results<Ok<IReadOnlyList<IssuedWalletPassResponse>>, ProblemHttpResult>
                > (
                    [FromQuery] Guid? personId,
                    [FromQuery] Guid? templateId,
                    [FromQuery] bool? activeOnly,
                    IQueryHandler<
                        GetIssuedWalletPassesQuery,
                        IReadOnlyList<IssuedWalletPassResponse>
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(
                        new GetIssuedWalletPassesQuery(personId, templateId, activeOnly ?? false),
                        ct
                    );
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("GetIssuedWalletPasses");

        _ = group
            .MapPost(
                "/issue",
                static async Task<
                    Results<Created<IssueWalletPassResultResponse>, ProblemHttpResult>
                > (
                    IssueWalletPassCommand command,
                    ICommandHandler<IssueWalletPassCommand, IssueWalletPassResultResponse> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(command, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Created(
                            $"/api/checkin/wallet-passes/{res.Value.Pass.Id:D}/preview",
                            res.Value
                        );
                }
            )
            .WithName("IssueWalletPass");
    }

    private static void MapDynamicRevokePreviewAndScanRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPut(
                "/{id:guid}/dynamic-fields",
                static async Task<Results<Ok<IssuedWalletPassResponse>, ProblemHttpResult>> (
                    Guid id,
                    UpdateIssuedWalletPassDynamicFieldsCommand body,
                    ICommandHandler<
                        UpdateIssuedWalletPassDynamicFieldsCommand,
                        IssuedWalletPassResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(body with { IssuedPassId = id }, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("UpdateIssuedWalletPassDynamicFields");

        _ = group
            .MapPost(
                "/{id:guid}/revoke",
                static async Task<Results<Ok<IssuedWalletPassResponse>, ProblemHttpResult>> (
                    Guid id,
                    RevokeIssuedWalletPassCommand body,
                    ICommandHandler<
                        RevokeIssuedWalletPassCommand,
                        IssuedWalletPassResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(body with { IssuedPassId = id }, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("RevokeIssuedWalletPass");

        MapPreviewAndScanRoutes(group);
    }

    private static void MapPreviewAndScanRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/{id:guid}/preview",
                static async Task<Results<Ok<WalletPassClaimBundleResponse>, ProblemHttpResult>> (
                    Guid id,
                    IQueryHandler<
                        PreviewIssuedWalletPassQuery,
                        WalletPassClaimBundleResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(new PreviewIssuedWalletPassQuery(id), ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("PreviewIssuedWalletPass");

        _ = group
            .MapPost(
                "/kiosk-scan",
                static async Task<Results<Ok<KioskWalletPassScanResponse>, ProblemHttpResult>> (
                    ResolveKioskWalletPassScanQuery query,
                    IQueryHandler<
                        ResolveKioskWalletPassScanQuery,
                        KioskWalletPassScanResponse
                    > handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var res = await handler.HandleAsync(query, ct);
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("ResolveKioskWalletPassScan");
    }

    private static void MapAnonymousClaimRoutes(IEndpointRouteBuilder endpoints)
    {
        var claim = endpoints
            .MapGroup("/api/checkin/wallet-passes/claim")
            .WithTags("CheckIn - Wallet Pass Claim")
            .AllowAnonymous();

        _ = claim
            .MapGet(
                "/{claimToken}",
                static async Task<Results<Ok<WalletPassClaimBundleResponse>, ProblemHttpResult>> (
                    string claimToken,
                    IQueryHandler<ClaimWalletPassQuery, WalletPassClaimBundleResponse> handler,
                    HttpContext ctx,
                    CancellationToken ct
                ) =>
                {
                    var origin = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
                    var res = await handler.HandleAsync(
                        new ClaimWalletPassQuery(claimToken, origin),
                        ct
                    );
                    return res.IsFailure
                        ? CheckInProblemResultMapper.ToProblemResult(res, ctx)
                        : TypedResults.Ok(res.Value);
                }
            )
            .WithName("ClaimWalletPassBundle");

        _ = claim
            .MapGet("/{claimToken}/pkpass", StreamClaimedPkpassAsync)
            .WithName("DownloadClaimedApplePkpass");
    }

    private static async Task<
        Results<FileContentHttpResult, ProblemHttpResult>
    > StreamClaimedPkpassAsync(
        string claimToken,
        IQueryHandler<ClaimWalletPassQuery, WalletPassClaimBundleResponse> handler,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var origin = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var res = await handler.HandleAsync(new ClaimWalletPassQuery(claimToken, origin), ct);
        if (res.IsFailure)
        {
            return CheckInProblemResultMapper.ToProblemResult(res, ctx);
        }

        var apple = res.Value.ApplePkpass;
        return apple?.BinaryPayload is null
            ? CheckInProblemResultMapper.ToProblemResult(
                Result.Failure(
                    DomainError.Validation(
                        "WalletPass.AppleNotConfigured",
                        "Apple .pkpass signing certificate is not configured on this instance."
                    )
                ),
                ctx
            )
            : TypedResults.File(apple.BinaryPayload, apple.ContentType, apple.FileName);
    }
}
