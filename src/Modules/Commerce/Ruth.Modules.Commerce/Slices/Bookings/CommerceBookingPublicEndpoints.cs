using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal static class CommerceBookingPublicEndpoints
{
    public static IEndpointRouteBuilder MapCommerceBookingPublicEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var publicPortal = endpoints
            .MapGroup("/api/commerce/bookings/public")
            .WithTags("Commerce Services, Rentals & Bookings")
            .AllowAnonymous();

        MapCatalogAndSlotRoutes(publicPortal);
        MapCheckoutAndConfirmationRoutes(publicPortal);
        return endpoints;
    }

    private static void MapCatalogAndSlotRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/services", ListPublicServicesAsync)
            .WithName("CommerceListPublicBookableServices")
            .WithSummary("List public bookable services")
            .WithDescription("Returns active public 1-on-1 services and workshops.")
            .Produces<IReadOnlyList<BookableServiceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/resources", ListPublicResourcesAsync)
            .WithName("CommerceListPublicRentableResources")
            .WithSummary("List public rentable resources")
            .WithDescription("Returns active public rooms, courts, studios, and equipment.")
            .Produces<IReadOnlyList<RentableResourceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/slots", QuerySlotsAsync)
            .WithName("CommerceQueryAvailableBookingSlots")
            .WithSummary("Query conflict-free available booking slots")
            .WithDescription(
                "Computes conflict-free slots honoring Schedule windows, buffers, blackouts, and TTL holds."
            )
            .Produces<IReadOnlyList<AvailableBookingSlotDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/holds", CreateHoldAsync)
            .WithName("CommerceCreateBookingSlotHold")
            .WithSummary("Acquire a TTL reservation hold on a candidate slot")
            .WithDescription("Reserves a candidate time slot during Booking Wizard checkout.")
            .Produces<BookingSlotHoldResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapCheckoutAndConfirmationRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/quote", PreviewQuoteAsync)
            .WithName("CommercePreviewBookingQuote")
            .WithSummary("Preview member-free, discounted, punch-pass, or paid booking quote")
            .WithDescription("Evaluates member benefits and pricing for a candidate slot.")
            .Produces<BookingQuoteResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/confirm", ConfirmBookingAsync)
            .WithName("CommerceConfirmBooking")
            .WithSummary("Confirm a service appointment or resource rental")
            .WithDescription(
                "Confirms slot, settles payment/invoice/punch-pass, emits ServiceBookingConfirmedIntegrationEvent, and syncs Wallet Passes."
            )
            .Produces<ServiceBookingResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/confirmations/{codeOrId}", GetConfirmationAsync)
            .WithName("CommerceGetBookingConfirmation")
            .WithSummary("Lookup booking by confirmation code or ID")
            .WithDescription("Returns confirmed or cancelled booking details.")
            .Produces<ServiceBookingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/confirmations/{bookingId:guid}/cancel", PublicCancelBookingAsync)
            .WithName("CommercePublicCancelBooking")
            .WithSummary("Self-service booking cancellation enforcing notice policy")
            .WithDescription("Cancels a booking when requested outside the minimum notice window.")
            .Produces<ServiceBookingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BookableServiceResponse>>, ProblemHttpResult>
    > ListPublicServicesAsync(
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetBookableServicesQuery,
            IReadOnlyList<BookableServiceResponse>
        >(new GetBookableServicesQuery(ActiveOnly: true, PublicOnly: true, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RentableResourceResponse>>, ProblemHttpResult>
    > ListPublicResourcesAsync(
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetRentableResourcesQuery,
            IReadOnlyList<RentableResourceResponse>
        >(new GetRentableResourcesQuery(ActiveOnly: true, PublicOnly: true, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AvailableBookingSlotDto>>, ProblemHttpResult>
    > QuerySlotsAsync(
        QueryAvailableBookingSlotsQuery body,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            QueryAvailableBookingSlotsQuery,
            IReadOnlyList<AvailableBookingSlotDto>
        >(body, ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<BookingSlotHoldResponse>, ProblemHttpResult>
    > CreateHoldAsync(
        CreateBookingSlotHoldCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateBookingSlotHoldCommand, BookingSlotHoldResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created(
                $"/api/commerce/bookings/public/holds/{r.Value.HoldToken}",
                r.Value
            )
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<BookingQuoteResponse>, ProblemHttpResult>
    > PreviewQuoteAsync(
        PreviewBookingQuoteQuery body,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<PreviewBookingQuoteQuery, BookingQuoteResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<ServiceBookingResponse>, ProblemHttpResult>
    > ConfirmBookingAsync(
        ConfirmBookingCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ConfirmBookingCommand, ServiceBookingResponse>(body, ct);
        return r.IsSuccess
            ? TypedResults.Created(
                $"/api/commerce/bookings/public/confirmations/{r.Value.ConfirmationCode}",
                r.Value
            )
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<ServiceBookingResponse>, ProblemHttpResult>
    > GetConfirmationAsync(
        string codeOrId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetServiceBookingByCodeOrIdQuery, ServiceBookingResponse>(
            new GetServiceBookingByCodeOrIdQuery(codeOrId),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<ServiceBookingResponse>, ProblemHttpResult>
    > PublicCancelBookingAsync(
        Guid bookingId,
        CancelBookingCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CancelBookingCommand, ServiceBookingResponse>(
            body with
            {
                BookingId = bookingId,
                StaffOverride = false,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }
}
