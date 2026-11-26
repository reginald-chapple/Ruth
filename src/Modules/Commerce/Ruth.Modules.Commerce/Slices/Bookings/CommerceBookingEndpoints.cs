using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal static class CommerceBookingEndpoints
{
    public static IEndpointRouteBuilder MapCommerceBookingEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var staff = endpoints
            .MapGroup("/api/commerce/bookings")
            .WithTags("Commerce Services, Rentals & Bookings")
            .RequireAuthorization();

        MapStaffSetupRoutes(staff);
        MapStaffCalendarRoutes(staff);
        _ = endpoints.MapCommerceBookingPublicEndpoints();
        return endpoints;
    }

    private static void MapStaffSetupRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/services", ListStaffServicesAsync)
            .WithName("CommerceListStaffBookableServices")
            .WithSummary("List bookable services")
            .WithDescription("Returns configured 1-on-1 services and workshops.")
            .Produces<IReadOnlyList<BookableServiceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/services", CreateServiceAsync)
            .WithName("CommerceCreateBookableService")
            .WithSummary("Create bookable service")
            .WithDescription("Creates a 1-on-1 appointment service or multi-seat workshop.")
            .Produces<BookableServiceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/services/{id:guid}", UpdateServiceAsync)
            .WithName("CommerceUpdateBookableService")
            .WithSummary("Update bookable service")
            .WithDescription("Updates duration, buffers, member pricing, and virtual room mode.")
            .Produces<BookableServiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapGet("/resources", ListStaffResourcesAsync)
            .WithName("CommerceListStaffRentableResources")
            .WithSummary("List rentable facility and equipment resources")
            .WithDescription("Returns configured rooms, courts, studios, and equipment.")
            .Produces<IReadOnlyList<RentableResourceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/resources", CreateResourceAsync)
            .WithName("CommerceCreateRentableResource")
            .WithSummary("Create rentable facility or equipment resource")
            .WithDescription("Creates a rentable resource linked to a Core Location.")
            .Produces<RentableResourceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/resources/{id:guid}", UpdateResourceAsync)
            .WithName("CommerceUpdateRentableResource")
            .WithSummary("Update rentable facility or equipment resource")
            .WithDescription("Updates hourly rates, member benefits, buffers, and horizon.")
            .Produces<RentableResourceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void MapStaffCalendarRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/rules", ListRulesAsync)
            .WithName("CommerceListAvailabilityRules")
            .WithSummary("List availability and blackout rules")
            .WithDescription("Returns recurring schedule windows and blackout exclusions.")
            .Produces<IReadOnlyList<AvailabilityRuleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/rules", CreateRuleAsync)
            .WithName("CommerceCreateAvailabilityRule")
            .WithSummary("Create availability or blackout rule")
            .WithDescription("Creates a recurring window, custom UTC window, or blackout window.")
            .Produces<AvailabilityRuleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/rules/{id:guid}", DeleteRuleAsync)
            .WithName("CommerceDeleteAvailabilityRule")
            .WithSummary("Delete availability or blackout rule")
            .WithDescription("Removes an availability or blackout rule.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/", ListBookingsAsync)
            .WithName("CommerceListServiceBookings")
            .WithSummary("Query Coordinator Calendar bookings")
            .WithDescription("Returns appointments and resource rentals over an optional window.")
            .Produces<IReadOnlyList<ServiceBookingResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/holds/expire", ExpireHoldsAsync)
            .WithName("CommerceExpireBookingSlotHolds")
            .WithSummary("Release expired TTL booking slot holds")
            .WithDescription("Deletes expired temporary slot reservation holds.")
            .Produces<BookingSlotHoldCleanupResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/{bookingId:guid}/cancel", StaffCancelBookingAsync)
            .WithName("CommerceStaffCancelBooking")
            .WithSummary("Cancel a booking (staff coordinator)")
            .WithDescription(
                "Cancels a booking, optionally refunding/voiding, and syncs Wallet Passes."
            )
            .Produces<ServiceBookingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<BookableServiceResponse>>, ProblemHttpResult>
    > ListStaffServicesAsync(
        bool? activeOnly,
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetBookableServicesQuery,
            IReadOnlyList<BookableServiceResponse>
        >(new GetBookableServicesQuery(activeOnly ?? false, PublicOnly: false, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<BookableServiceResponse>, ProblemHttpResult>
    > CreateServiceAsync(
        BookableServiceSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateBookableServiceCommand, BookableServiceResponse>(
            new CreateBookableServiceCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/commerce/bookings/services/{r.Value.Id:D}", r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<BookableServiceResponse>, ProblemHttpResult>
    > UpdateServiceAsync(
        Guid id,
        BookableServiceSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<UpdateBookableServiceCommand, BookableServiceResponse>(
            new UpdateBookableServiceCommand(id, spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RentableResourceResponse>>, ProblemHttpResult>
    > ListStaffResourcesAsync(
        bool? activeOnly,
        Guid? branchId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetRentableResourcesQuery,
            IReadOnlyList<RentableResourceResponse>
        >(new GetRentableResourcesQuery(activeOnly ?? false, PublicOnly: false, branchId), ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<RentableResourceResponse>, ProblemHttpResult>
    > CreateResourceAsync(
        RentableResourceSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateRentableResourceCommand, RentableResourceResponse>(
            new CreateRentableResourceCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/commerce/bookings/resources/{r.Value.Id:D}", r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<RentableResourceResponse>, ProblemHttpResult>
    > UpdateResourceAsync(
        Guid id,
        RentableResourceSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<UpdateRentableResourceCommand, RentableResourceResponse>(
            new UpdateRentableResourceCommand(id, spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AvailabilityRuleResponse>>, ProblemHttpResult>
    > ListRulesAsync(
        Guid? bookableServiceId,
        Guid? rentableResourceId,
        Guid? providerPersonId,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetAvailabilityRulesQuery,
            IReadOnlyList<AvailabilityRuleResponse>
        >(
            new GetAvailabilityRulesQuery(bookableServiceId, rentableResourceId, providerPersonId),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<AvailabilityRuleResponse>, ProblemHttpResult>
    > CreateRuleAsync(
        AvailabilityRuleSpec spec,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateAvailabilityRuleCommand, AvailabilityRuleResponse>(
            new CreateAvailabilityRuleCommand(spec),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/commerce/bookings/rules/{r.Value.Id:D}", r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteRuleAsync(
        Guid id,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync(new DeleteAvailabilityRuleCommand(id), ct);
        return r.IsSuccess
            ? TypedResults.NoContent()
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<ServiceBookingResponse>>, ProblemHttpResult>
    > ListBookingsAsync(
        DateTimeOffset? rangeStartUtc,
        DateTimeOffset? rangeEndUtc,
        Guid? bookableServiceId,
        Guid? rentableResourceId,
        Guid? providerPersonId,
        Guid? customerPersonId,
        ServiceBookingStatus? status,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetServiceBookingsQuery, IReadOnlyList<ServiceBookingResponse>>(
            new GetServiceBookingsQuery(
                rangeStartUtc,
                rangeEndUtc,
                bookableServiceId,
                rentableResourceId,
                providerPersonId,
                customerPersonId,
                status
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<BookingSlotHoldCleanupResponse>, ProblemHttpResult>
    > ExpireHoldsAsync(ICommandDispatcher d, HttpContext ctx, CancellationToken ct)
    {
        var r = await d.SendAsync<ExpireBookingSlotHoldsCommand, BookingSlotHoldCleanupResponse>(
            new ExpireBookingSlotHoldsCommand(),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<ServiceBookingResponse>, ProblemHttpResult>
    > StaffCancelBookingAsync(
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
                StaffOverride = true,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : CommerceProblemResultMapper.ToProblemResult(r, ctx);
    }
}
