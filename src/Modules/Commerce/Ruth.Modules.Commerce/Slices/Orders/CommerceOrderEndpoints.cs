using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal static class CommerceOrderEndpoints
{
    public static IEndpointRouteBuilder MapCommerceOrderEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var storefront = endpoints
            .MapGroup("/api/commerce/storefront")
            .WithTags("Commerce Storefront & Checkout")
            .AllowAnonymous();

        MapStorefrontCheckoutRoutes(storefront);

        var staffOrders = endpoints
            .MapGroup("/api/commerce/orders")
            .WithTags("Commerce Orders & Punch Passes")
            .RequireAuthorization();

        MapStaffOrderRoutes(staffOrders);

        var staffPasses = endpoints
            .MapGroup("/api/commerce/punch-passes")
            .WithTags("Commerce Orders & Punch Passes")
            .RequireAuthorization();

        MapPunchPassRoutes(staffPasses);
        return endpoints;
    }

    private static void MapStorefrontCheckoutRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/quote", PreviewQuoteAsync)
            .WithName("CommercePreviewOrderQuote")
            .WithSummary("Preview cart pricing, member discounts, and IRS quid-pro-quo splits")
            .WithDescription(
                "Evaluates automatic member discounts, tax, shipping, and non-deductible vs charitable splits."
            )
            .Produces<CommerceOrderQuoteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/checkout", SubmitOrderAsync)
            .WithName("CommerceSubmitOrder")
            .WithSummary("Submit storefront order and settle payment or invoice")
            .WithDescription(
                "Settles splits via Finance.Contracts, deducts inventory, issues punch passes, and syncs Wallet Passes."
            )
            .Produces<CommerceOrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/orders/{orderNumberOrId}", GetOrderAsync)
            .WithName("CommerceGetOrderConfirmation")
            .WithSummary("Lookup commerce order by order number or ID")
            .WithDescription("Returns order status, line items, and quid-pro-quo receipt summary.")
            .Produces<CommerceOrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet(
                "/orders/{orderNumberOrId}/lines/{lineId:guid}/download",
                GetDigitalDownloadAsync
            )
            .WithName("CommerceGetOrderDigitalDownloadLink")
            .WithSummary("Generate RustFS presigned download link for a digital order line")
            .WithDescription("Returns a time-bounded presigned URL for a settled digital product.")
            .Produces<OrderDigitalDownloadLinkResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapStaffOrderRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListOrdersAsync)
            .WithName("CommerceListOrders")
            .WithSummary("List commerce orders")
            .WithDescription("Returns orders filtered by buyer, status, or branch.")
            .Produces<IReadOnlyList<CommerceOrderResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{orderId:guid}/fulfillment", UpdateFulfillmentAsync)
            .WithName("CommerceUpdateOrderFulfillment")
            .WithSummary("Advance physical order fulfillment status")
            .WithDescription("Transitions a paid order to ReadyForPickup, Shipped, or Fulfilled.")
            .Produces<CommerceOrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{orderId:guid}/refund", RefundOrderAsync)
            .WithName("CommerceRefundOrder")
            .WithSummary("Process full or partial order refund")
            .WithDescription(
                "Refunds transaction via Finance.Contracts, reverses punch passes, and syncs Wallet Passes."
            )
            .Produces<CommerceOrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapPunchPassRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListIssuedPunchPassesAsync)
            .WithName("CommerceListIssuedPunchPasses")
            .WithSummary("List issued punch passes")
            .WithDescription("Returns issued punch passes and balance ledger entries.")
            .Produces<IReadOnlyList<IssuedPunchPassResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", IssuePunchPassAsync)
            .WithName("CommerceIssuePunchPass")
            .WithSummary("Issue a punch pass to a constituent")
            .WithDescription("Issues a punch pass and syncs active Smart Wallet Passes.")
            .Produces<IssuedPunchPassResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{passId:guid}/redeem", RedeemPunchPassAsync)
            .WithName("CommerceRedeemPunchPassCredits")
            .WithSummary("Redeem credits from an issued punch pass")
            .WithDescription(
                "Deducts credits (supporting household sharing) and syncs Smart Wallet Passes."
            )
            .Produces<IssuedPunchPassResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{passId:guid}/adjust", AdjustPunchPassAsync)
            .WithName("CommerceAdjustPunchPassBalance")
            .WithSummary("Adjust punch pass credit balance")
            .WithDescription(
                "Records a manual credit/debit ledger adjustment and syncs Wallet Passes."
            )
            .Produces<IssuedPunchPassResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<CommerceOrderQuoteResponse>, ProblemHttpResult>
    > PreviewQuoteAsync(
        PreviewCommerceOrderQuoteQuery body,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            PreviewCommerceOrderQuoteQuery,
            CommerceOrderQuoteResponse
        >(body, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<CommerceOrderResponse>, ProblemHttpResult>
    > SubmitOrderAsync(
        SubmitCommerceOrderCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<SubmitCommerceOrderCommand, CommerceOrderResponse>(
            body,
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/commerce/storefront/orders/{res.Value.OrderNumber}",
                res.Value
            )
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<Results<Ok<CommerceOrderResponse>, ProblemHttpResult>> GetOrderAsync(
        string orderNumberOrId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetCommerceOrderByNumberOrIdQuery,
            CommerceOrderResponse
        >(new GetCommerceOrderByNumberOrIdQuery(orderNumberOrId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<OrderDigitalDownloadLinkResponse>, ProblemHttpResult>
    > GetDigitalDownloadAsync(
        string orderNumberOrId,
        Guid lineId,
        int? expiresInMinutes,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetOrderDigitalDownloadLinkQuery,
            OrderDigitalDownloadLinkResponse
        >(
            new GetOrderDigitalDownloadLinkQuery(orderNumberOrId, lineId, expiresInMinutes ?? 15),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommerceOrderResponse>>, ProblemHttpResult>
    > ListOrdersAsync(
        Guid? buyerPersonId,
        CommerceOrderStatus? status,
        Guid? branchId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetCommerceOrdersQuery,
            IReadOnlyList<CommerceOrderResponse>
        >(new GetCommerceOrdersQuery(buyerPersonId, status, branchId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<CommerceOrderResponse>, ProblemHttpResult>
    > UpdateFulfillmentAsync(
        Guid orderId,
        UpdateCommerceOrderFulfillmentCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateCommerceOrderFulfillmentCommand,
            CommerceOrderResponse
        >(body with { OrderId = orderId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<CommerceOrderResponse>, ProblemHttpResult>
    > RefundOrderAsync(
        Guid orderId,
        RefundCommerceOrderCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<RefundCommerceOrderCommand, CommerceOrderResponse>(
            body with
            {
                OrderId = orderId,
            },
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<IssuedPunchPassResponse>>, ProblemHttpResult>
    > ListIssuedPunchPassesAsync(
        Guid? ownerPersonId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetIssuedPunchPassesQuery,
            IReadOnlyList<IssuedPunchPassResponse>
        >(new GetIssuedPunchPassesQuery(ownerPersonId, activeOnly ?? false), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<IssuedPunchPassResponse>, ProblemHttpResult>
    > IssuePunchPassAsync(
        IssuePunchPassCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<IssuePunchPassCommand, IssuedPunchPassResponse>(
            body,
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created($"/api/commerce/punch-passes/{res.Value.Id:D}", res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IssuedPunchPassResponse>, ProblemHttpResult>
    > RedeemPunchPassAsync(
        Guid passId,
        RedeemPunchPassCreditsCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            RedeemPunchPassCreditsCommand,
            IssuedPunchPassResponse
        >(body with { IssuedPunchPassId = passId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IssuedPunchPassResponse>, ProblemHttpResult>
    > AdjustPunchPassAsync(
        Guid passId,
        AdjustPunchPassBalanceCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            AdjustPunchPassBalanceCommand,
            IssuedPunchPassResponse
        >(body with { IssuedPunchPassId = passId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
