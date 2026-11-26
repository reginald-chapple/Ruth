using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain.Bookings;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal static class BookingsMapper
{
    public static BookableServiceResponse MapService(BookableService s)
    {
        return new(
            s.Id.Value,
            s.Name,
            s.Slug,
            s.Summary,
            s.Description,
            s.DeliveryMode,
            s.DurationMinutes,
            s.SlotIncrementMinutes,
            s.SetupBufferMinutes,
            s.TeardownBufferMinutes,
            s.MaxParticipantsPerSlot,
            s.MinimumLeadTimeMinutes,
            s.MaximumAdvanceDays,
            s.CancellationNoticeMinutes,
            s.CurrencyCode,
            s.StandardPrice,
            s.IsFreeForMembers,
            s.MemberOverridePrice,
            s.FreeOrDiscountedMemberGroupId,
            s.FreeOrDiscountedMemberTier,
            s.AllowPunchPassRedemption,
            s.PunchPassCreditsRequired,
            s.RevenueAccountId,
            s.DefaultFinancialGatewayId,
            s.AllowInvoiceBilling,
            s.BranchId,
            s.DefaultLocationId,
            [.. s.AssignedProviderPersonIds],
            s.IsActive,
            s.IsPublic,
            new Dictionary<string, string?>(s.AttributeValues, StringComparer.OrdinalIgnoreCase),
            s.CreatedOnUtc,
            s.ModifiedOnUtc
        );
    }

    public static RentableResourceResponse MapResource(RentableResource r)
    {
        return new(
            r.Id.Value,
            r.Name,
            r.Slug,
            r.Description,
            r.ResourceKind,
            r.CoreLocationId,
            r.BranchId,
            r.QuantityAvailable,
            r.MinDurationMinutes,
            r.MaxDurationMinutes,
            r.SlotIncrementMinutes,
            r.SetupBufferMinutes,
            r.TeardownBufferMinutes,
            r.MinimumLeadTimeMinutes,
            r.MaximumAdvanceDays,
            r.CancellationNoticeMinutes,
            r.CurrencyCode,
            r.HourlyRate,
            r.FlatCleaningOrSetupFee,
            r.IsFreeForMembers,
            r.MemberHourlyRate,
            r.FreeOrDiscountedMemberGroupId,
            r.FreeOrDiscountedMemberTier,
            r.RevenueAccountId,
            r.DefaultFinancialGatewayId,
            r.AllowInvoiceBilling,
            r.IsActive,
            r.IsPublic,
            new Dictionary<string, string?>(r.AttributeValues, StringComparer.OrdinalIgnoreCase),
            r.CreatedOnUtc,
            r.ModifiedOnUtc
        );
    }

    public static AvailabilityRuleResponse MapRule(AvailabilityRule rule)
    {
        return new(
            rule.Id.Value,
            rule.Name,
            rule.TargetKind,
            rule.RuleType,
            rule.BookableServiceId?.Value,
            rule.RentableResourceId?.Value,
            rule.ProviderPersonId,
            rule.LocationId,
            rule.ScheduleId,
            rule.DayOfWeek,
            rule.StartLocalTime,
            rule.EndLocalTime,
            rule.TimeZoneId,
            rule.CustomStartUtc,
            rule.CustomEndUtc,
            rule.IsActive,
            rule.CreatedOnUtc
        );
    }

    public static BookingSlotHoldResponse MapHold(BookingSlotHold h)
    {
        return new(
            h.Id.Value,
            h.HoldToken,
            h.TargetKind,
            h.BookableServiceId?.Value,
            h.RentableResourceId?.Value,
            h.ProviderPersonId,
            h.LocationId,
            h.SlotStartUtc,
            h.SlotEndUtc,
            h.EffectiveBlockStartUtc,
            h.EffectiveBlockEndUtc,
            h.Quantity,
            h.ExpiresAtUtc
        );
    }

    public static ServiceBookingResponse MapBooking(ServiceOrResourceBooking b)
    {
        return new(
            b.Id.Value,
            b.ConfirmationCode,
            b.Status,
            b.TargetKind,
            b.BookableServiceId?.Value,
            b.RentableResourceId?.Value,
            b.TargetName,
            b.DeliveryMode,
            b.RequiresVirtualRoom,
            b.VirtualRoomReferenceKey,
            b.ProviderPersonId,
            b.LocationId,
            b.BranchId,
            b.CustomerPersonId,
            b.CustomerName,
            b.CustomerEmail,
            b.CustomerPhone,
            b.CustomerNotes,
            b.ParticipantOrUnitCount,
            b.StartUtc,
            b.EndUtc,
            b.EffectiveBlockStartUtc,
            b.EffectiveBlockEndUtc,
            b.PricingKind,
            b.CurrencyCode,
            b.ListAmount,
            b.DiscountAmount,
            b.TotalAmount,
            b.PaidAmount,
            b.LinkedFinancialTransactionId,
            b.LinkedInvoiceId,
            b.RedeemedPunchPassId?.Value,
            b.CreatedOnUtc,
            b.CancelledAtUtc,
            b.CancellationReason
        );
    }
}
