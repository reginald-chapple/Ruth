using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal static class CommerceCatalogEndpoints
{
    public static IEndpointRouteBuilder MapCommerceCatalogEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var staff = endpoints
            .MapGroup("/api/commerce/catalog")
            .WithTags("Commerce Catalog & Member Pricing")
            .RequireAuthorization();

        MapPunchPassTypeRoutes(staff);
        MapStaffProductRoutes(staff);
        MapMemberPricingRoutes(staff);

        var storefront = endpoints
            .MapGroup("/api/commerce/storefront")
            .WithTags("Commerce Storefront & Checkout")
            .AllowAnonymous();

        _ = storefront
            .MapGet("/products", ListStorefrontProductsAsync)
            .WithName("CommerceListStorefrontProducts")
            .WithSummary("List public storefront products")
            .WithDescription("Returns active public products and purchasable SKU variants.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK);

        _ = storefront
            .MapGet("/products/{idOrSlug}", GetProductAsync)
            .WithName("CommerceGetStorefrontProduct")
            .WithSummary("Get product by ID or slug")
            .WithDescription("Returns product details, variants, and quid-pro-quo metadata.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static void MapPunchPassTypeRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/punch-pass-types", ListPunchPassTypesAsync)
            .WithName("CommerceListPunchPassTypes")
            .WithSummary("List punch pass and credit pack types")
            .WithDescription("Returns configured multi-visit and credit pack types.")
            .Produces<IReadOnlyList<PunchPassTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/punch-pass-types", CreatePunchPassTypeAsync)
            .WithName("CommerceCreatePunchPassType")
            .WithSummary("Create punch pass or credit pack type")
            .WithDescription("Creates a multi-visit facility pass or service credit pack type.")
            .Produces<PunchPassTypeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/punch-pass-types/{id:guid}", UpdatePunchPassTypeAsync)
            .WithName("CommerceUpdatePunchPassType")
            .WithSummary("Update punch pass or credit pack type")
            .WithDescription("Updates credit count, validity window, and sharing rules.")
            .Produces<PunchPassTypeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapStaffProductRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/products", ListStaffProductsAsync)
            .WithName("CommerceListStaffProducts")
            .WithSummary("List catalog products")
            .WithDescription("Returns catalog products filtered by active state, kind, or branch.")
            .Produces<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/products", CreateProductAsync)
            .WithName("CommerceCreateProduct")
            .WithSummary("Create catalog product and SKU variants")
            .WithDescription(
                "Creates a physical, digital RustFS, or punch-pass product with optional IRS quid-pro-quo split settings."
            )
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/products/{id:guid}", UpdateProductAsync)
            .WithName("CommerceUpdateProduct")
            .WithSummary("Update catalog product and reconcile variants")
            .WithDescription("Updates product settings, accounts, tax rates, and SKU variants.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapMemberPricingRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/member-pricing-rules", ListRulesAsync)
            .WithName("CommerceListMemberPricingRules")
            .WithSummary("List automatic member pricing rules")
            .WithDescription("Returns tier and group automatic discount rules.")
            .Produces<IReadOnlyList<MemberPricingRuleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/member-pricing-rules", CreateRuleAsync)
            .WithName("CommerceCreateMemberPricingRule")
            .WithSummary("Create automatic member pricing rule")
            .WithDescription("Creates a tier or group automatic discount rule.")
            .Produces<MemberPricingRuleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/member-pricing-rules/{id:guid}", UpdateRuleAsync)
            .WithName("CommerceUpdateMemberPricingRule")
            .WithSummary("Update automatic member pricing rule")
            .WithDescription("Updates an existing member pricing discount rule.")
            .Produces<MemberPricingRuleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PunchPassTypeResponse>>, ProblemHttpResult>
    > ListPunchPassTypesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetPunchPassTypesQuery,
            IReadOnlyList<PunchPassTypeResponse>
        >(new GetPunchPassTypesQuery(activeOnly ?? false), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<PunchPassTypeResponse>, ProblemHttpResult>
    > CreatePunchPassTypeAsync(
        PunchPassTypeSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<CreatePunchPassTypeCommand, PunchPassTypeResponse>(
            new CreatePunchPassTypeCommand(spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/commerce/catalog/punch-pass-types/{res.Value.Id:D}",
                res.Value
            )
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<PunchPassTypeResponse>, ProblemHttpResult>
    > UpdatePunchPassTypeAsync(
        Guid id,
        PunchPassTypeSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<UpdatePunchPassTypeCommand, PunchPassTypeResponse>(
            new UpdatePunchPassTypeCommand(id, spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ProductResponse>>, ProblemHttpResult>
    > ListStaffProductsAsync(
        bool? activeOnly,
        ProductKind? kind,
        Guid? branchId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetProductsQuery, IReadOnlyList<ProductResponse>>(
            new GetProductsQuery(activeOnly ?? false, PublicOnly: false, kind, branchId),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ProductResponse>>, ProblemHttpResult>
    > ListStorefrontProductsAsync(
        ProductKind? kind,
        Guid? branchId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetProductsQuery, IReadOnlyList<ProductResponse>>(
            new GetProductsQuery(ActiveOnly: true, PublicOnly: true, kind, branchId),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> GetProductAsync(
        string idOrSlug,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<GetProductByIdOrSlugQuery, ProductResponse>(
            new GetProductByIdOrSlugQuery(idOrSlug),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<ProductResponse>, ProblemHttpResult>
    > CreateProductAsync(
        ProductSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<CreateProductCommand, ProductResponse>(
            new CreateProductCommand(spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Created($"/api/commerce/storefront/products/{res.Value.Slug}", res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<Results<Ok<ProductResponse>, ProblemHttpResult>> UpdateProductAsync(
        Guid id,
        ProductSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<UpdateProductCommand, ProductResponse>(
            new UpdateProductCommand(id, spec),
            cancellationToken
        );
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<MemberPricingRuleResponse>>, ProblemHttpResult>
    > ListRulesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetMemberPricingRulesQuery,
            IReadOnlyList<MemberPricingRuleResponse>
        >(new GetMemberPricingRulesQuery(activeOnly ?? false), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<MemberPricingRuleResponse>, ProblemHttpResult>
    > CreateRuleAsync(
        MemberPricingRuleSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateMemberPricingRuleCommand,
            MemberPricingRuleResponse
        >(new CreateMemberPricingRuleCommand(spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/commerce/catalog/member-pricing-rules/{res.Value.Id:D}",
                res.Value
            )
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<MemberPricingRuleResponse>, ProblemHttpResult>
    > UpdateRuleAsync(
        Guid id,
        MemberPricingRuleSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            UpdateMemberPricingRuleCommand,
            MemberPricingRuleResponse
        >(new UpdateMemberPricingRuleCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : CommerceProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
