using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class ConfirmBookingCommandHandler(
    CommerceDbContext dbContext,
    BookingScheduleAndConflictLoader loader,
    MemberEligibilityResolver eligibilityResolver,
    BookingSettlementCollaborator settlementCollaborator,
    IClock clock
) : ICommandHandler<ConfirmBookingCommand, ServiceBookingResponse>
{
    public async Task<Result<ServiceBookingResponse>> HandleAsync(
        ConfirmBookingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = clock.UtcNow;
        var resolved = await ResolveSlotAndHoldAsync(command, nowUtc, cancellationToken);
        if (resolved.IsFailure)
        {
            return Result.Failure<ServiceBookingResponse>(resolved.Errors);
        }

        var (ctx, slot, hold) = resolved.Value;
        var quote = await EvaluateBookingQuoteAsync(command, ctx, cancellationToken);
        var bookingRes = CreateBookingAggregate(command, ctx, slot, quote, nowUtc);
        if (bookingRes.IsFailure)
        {
            return Result.Failure<ServiceBookingResponse>(bookingRes.Errors);
        }

        var booking = bookingRes.Value;
        var settled = await settlementCollaborator.SettleBookingAsync(
            booking,
            quote,
            ctx.Service?.RevenueAccountId ?? ctx.Resource?.RevenueAccountId,
            ctx.Service?.DefaultFinancialGatewayId ?? ctx.Resource?.DefaultFinancialGatewayId,
            command,
            nowUtc,
            cancellationToken
        );
        if (settled.IsFailure)
        {
            return Result.Failure<ServiceBookingResponse>(settled.Errors);
        }

        await PersistConfirmationAndSyncAsync(booking, hold, nowUtc, cancellationToken);
        return BookingsMapper.MapBooking(booking);
    }

    private async Task<
        Result<(
            ResolvedTargetScheduleContext Context,
            AvailableBookingSlotDto Slot,
            BookingSlotHold? Hold
        )>
    > ResolveSlotAndHoldAsync(
        ConfirmBookingCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var startUtc = command.SlotStartUtc.ToUniversalTime();
        var ctxRes = await loader.BuildSlotContextAsync(
            command.TargetKind,
            command.TargetIdOrSlug,
            command.RequestedDurationMinutes,
            command.Quantity,
            command.ProviderPersonId,
            startUtc.AddHours(-12),
            startUtc.AddHours(36),
            command.HoldToken,
            nowUtc,
            cancellationToken
        );
        if (ctxRes.IsFailure)
        {
            return Result.Failure<(
                ResolvedTargetScheduleContext,
                AvailableBookingSlotDto,
                BookingSlotHold?
            )>(ctxRes.Errors);
        }

        var ctx = ctxRes.Value;
        var slot = BookingSlotEngine.ValidateExactSlot(
            ctx.Parameters,
            startUtc,
            ctx.OpenWindows,
            ctx.BlackoutWindows,
            ctx.OccupiedBlocks,
            nowUtc
        );
        if (slot is null)
        {
            return CommerceErrors.SlotUnavailable;
        }

        var holdRes = await ValidateOptionalHoldAsync(
            command.HoldToken,
            ctx,
            slot,
            command.Quantity,
            nowUtc,
            cancellationToken
        );
        return holdRes.IsFailure
            ? Result.Failure<(
                ResolvedTargetScheduleContext,
                AvailableBookingSlotDto,
                BookingSlotHold?
            )>(holdRes.Errors)
            : (ctx, slot, holdRes.Value);
    }

    private async Task<Result<BookingSlotHold?>> ValidateOptionalHoldAsync(
        string? holdToken,
        ResolvedTargetScheduleContext ctx,
        AvailableBookingSlotDto slot,
        int quantity,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(holdToken))
        {
            return Result.Success<BookingSlotHold?>(value: null);
        }

        var trimmed = holdToken.Trim();
        var hold = await dbContext.BookingSlotHolds.FirstOrDefaultAsync(
            h => h.HoldToken == trimmed,
            cancellationToken
        );
        return
            hold is null
            || !hold.IsActiveAt(nowUtc)
            || !hold.MatchesCandidate(
                ctx.Parameters.TargetKind,
                ctx.Service?.Id,
                ctx.Resource?.Id,
                slot.SlotStartUtc,
                slot.SlotEndUtc,
                quantity
            )
            ? CommerceErrors.HoldExpiredOrInvalid
            : Result.Success<BookingSlotHold?>(hold);
    }

    private async Task<BookingQuoteResponse> EvaluateBookingQuoteAsync(
        ConfirmBookingCommand command,
        ResolvedTargetScheduleContext ctx,
        CancellationToken cancellationToken
    )
    {
        var profile = await eligibilityResolver.ResolveAsync(
            command.CustomerPersonId,
            command.ExplicitMembershipTier,
            cancellationToken
        );

        return ctx.Service is not null
            ? BookingPricingCalculator.EvaluateServiceQuote(
                ctx.Service,
                command.Quantity,
                profile.Tiers,
                profile.GroupIds,
                command.RedeemPunchPassId is { } p && p != Guid.Empty
            )
            : BookingPricingCalculator.EvaluateResourceQuote(
                ctx.Resource!,
                ctx.Parameters.DurationMinutes,
                command.Quantity,
                profile.Tiers,
                profile.GroupIds
            );
    }

    private static Result<ServiceOrResourceBooking> CreateBookingAggregate(
        ConfirmBookingCommand command,
        ResolvedTargetScheduleContext ctx,
        AvailableBookingSlotDto slot,
        BookingQuoteResponse quote,
        DateTimeOffset nowUtc
    )
    {
        var name = ctx.Service?.Name ?? ctx.Resource!.Name;
        var mode = ctx.Service?.DeliveryMode ?? BookingDeliveryMode.InPerson;
        var branchId = ctx.Service?.BranchId ?? ctx.Resource?.BranchId;

        return ServiceOrResourceBooking.Create(
            ctx.Parameters.TargetKind,
            ctx.Service?.Id,
            ctx.Resource?.Id,
            name,
            mode,
            branchId,
            slot,
            command.Quantity,
            command.CustomerPersonId,
            command.CustomerName,
            command.CustomerEmail,
            command.CustomerPhone,
            command.CustomerNotes,
            quote,
            nowUtc
        );
    }

    private async Task PersistConfirmationAndSyncAsync(
        ServiceOrResourceBooking booking,
        BookingSlotHold? hold,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        hold?.MarkCompleted(booking.Id);
        _ = dbContext.ServiceBookings.Add(booking);
        dbContext.EnqueueIntegrationEvent(
            new ServiceBookingConfirmedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                booking.Id.Value,
                booking.ConfirmationCode,
                booking.TargetKind,
                booking.BookableServiceId?.Value,
                booking.RentableResourceId?.Value,
                booking.TargetName,
                booking.DeliveryMode,
                booking.CustomerPersonId,
                booking.CustomerName,
                booking.CustomerEmail,
                booking.ProviderPersonId,
                booking.LocationId,
                booking.BranchId,
                booking.StartUtc,
                booking.EndUtc,
                booking.ParticipantOrUnitCount,
                booking.CurrencyCode,
                booking.TotalAmount,
                booking.PaidAmount,
                booking.LinkedInvoiceId
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
