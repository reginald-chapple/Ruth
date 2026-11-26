using Ruth.Modules.Commerce.Contracts.Bookings;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>First-principles pricing evaluator resolving member-free, member-discounted, punch-pass, complimentary, and standard paid bookings.</summary>
public static class BookingPricingCalculator
{
    public static BookingQuoteResponse EvaluateServiceQuote(
        BookableService service,
        int quantity,
        IReadOnlySet<string> customerTiers,
        IReadOnlySet<Guid> customerGroupIds,
        bool redeemPunchPassRequested
    )
    {
        ArgumentNullException.ThrowIfNull(service);

        var listTotal = decimal.Round(
            service.StandardPrice * quantity,
            2,
            MidpointRounding.AwayFromZero
        );
        if (listTotal == 0m)
        {
            return BuildQuote(
                BookingTargetKind.Service,
                service.Id.Value,
                service.Name,
                service.CurrencyCode,
                BookingPricingKind.Complimentary,
                0m,
                0m,
                0,
                service.AllowInvoiceBilling
            );
        }

        if (redeemPunchPassRequested && service.AllowPunchPassRedemption)
        {
            return BuildQuote(
                BookingTargetKind.Service,
                service.Id.Value,
                service.Name,
                service.CurrencyCode,
                BookingPricingKind.PunchPassRedeemed,
                listTotal,
                0m,
                service.PunchPassCreditsRequired * quantity,
                service.AllowInvoiceBilling
            );
        }

        var memberDiscountedTotal = service.MemberOverridePrice is { } memberUnit
            ? decimal.Round(memberUnit * quantity, 2, MidpointRounding.AwayFromZero)
            : (decimal?)null;

        return ResolveMemberOrStandardQuote(
            BookingTargetKind.Service,
            service.Id.Value,
            service.Name,
            service.CurrencyCode,
            listTotal,
            service.IsFreeForMembers,
            memberDiscountedTotal,
            service.FreeOrDiscountedMemberTier,
            service.FreeOrDiscountedMemberGroupId,
            customerTiers,
            customerGroupIds,
            service.AllowInvoiceBilling
        );
    }

    public static BookingQuoteResponse EvaluateResourceQuote(
        RentableResource resource,
        int durationMinutes,
        int quantity,
        IReadOnlySet<string> customerTiers,
        IReadOnlySet<Guid> customerGroupIds
    )
    {
        ArgumentNullException.ThrowIfNull(resource);

        var hours = durationMinutes / 60m;
        var listTotal = decimal.Round(
            (resource.HourlyRate * hours * quantity) + resource.FlatCleaningOrSetupFee,
            2,
            MidpointRounding.AwayFromZero
        );

        if (listTotal == 0m)
        {
            return BuildQuote(
                BookingTargetKind.Resource,
                resource.Id.Value,
                resource.Name,
                resource.CurrencyCode,
                BookingPricingKind.Complimentary,
                0m,
                0m,
                0,
                resource.AllowInvoiceBilling
            );
        }

        var memberDiscountedTotal = resource.MemberHourlyRate is { } memberHourly
            ? decimal.Round(
                (memberHourly * hours * quantity) + resource.FlatCleaningOrSetupFee,
                2,
                MidpointRounding.AwayFromZero
            )
            : (decimal?)null;

        return ResolveMemberOrStandardQuote(
            BookingTargetKind.Resource,
            resource.Id.Value,
            resource.Name,
            resource.CurrencyCode,
            listTotal,
            resource.IsFreeForMembers,
            memberDiscountedTotal,
            resource.FreeOrDiscountedMemberTier,
            resource.FreeOrDiscountedMemberGroupId,
            customerTiers,
            customerGroupIds,
            resource.AllowInvoiceBilling
        );
    }

    private static BookingQuoteResponse ResolveMemberOrStandardQuote(
        BookingTargetKind targetKind,
        Guid targetId,
        string targetName,
        string currencyCode,
        decimal listTotal,
        bool isFreeForMembers,
        decimal? memberDiscountedTotal,
        string? requiredTier,
        Guid? requiredGroupId,
        IReadOnlySet<string> customerTiers,
        IReadOnlySet<Guid> customerGroupIds,
        bool allowInvoiceBilling
    )
    {
        var qualifies = QualifiesForMemberBenefit(
            isFreeForMembers || memberDiscountedTotal.HasValue,
            requiredTier,
            requiredGroupId,
            customerTiers,
            customerGroupIds
        );

        return qualifies && isFreeForMembers
                ? BuildQuote(
                    targetKind,
                    targetId,
                    targetName,
                    currencyCode,
                    BookingPricingKind.MemberFree,
                    listTotal,
                    0m,
                    0,
                    allowInvoiceBilling
                )
            : qualifies && memberDiscountedTotal is { } discounted
                ? BuildQuote(
                    targetKind,
                    targetId,
                    targetName,
                    currencyCode,
                    BookingPricingKind.MemberDiscounted,
                    listTotal,
                    discounted,
                    0,
                    allowInvoiceBilling
                )
            : BuildQuote(
                targetKind,
                targetId,
                targetName,
                currencyCode,
                BookingPricingKind.StandardPaid,
                listTotal,
                listTotal,
                0,
                allowInvoiceBilling
            );
    }

    private static BookingQuoteResponse BuildQuote(
        BookingTargetKind kind,
        Guid targetId,
        string targetName,
        string currency,
        BookingPricingKind pricingKind,
        decimal listAmount,
        decimal totalAmount,
        int punchCredits,
        bool allowInvoice
    )
    {
        return new BookingQuoteResponse(
            kind,
            targetId,
            targetName,
            currency,
            pricingKind,
            listAmount,
            listAmount - totalAmount,
            totalAmount,
            punchCredits,
            allowInvoice
        );
    }

    private static bool QualifiesForMemberBenefit(
        bool benefitConfigured,
        string? requiredTier,
        Guid? requiredGroupId,
        IReadOnlySet<string> customerTiers,
        IReadOnlySet<Guid> customerGroupIds
    )
    {
        if (!benefitConfigured)
        {
            return false;
        }

        var tierSpecific = !string.IsNullOrWhiteSpace(requiredTier);
        var groupSpecific = requiredGroupId.HasValue;
        if (!tierSpecific && !groupSpecific)
        {
            return customerTiers.Count > 0 || customerGroupIds.Count > 0;
        }

        var tierMatch = !tierSpecific || customerTiers.Contains(requiredTier!.Trim());
        var groupMatch = !groupSpecific || customerGroupIds.Contains(requiredGroupId!.Value);
        return tierMatch && groupMatch;
    }
}
