using System.Globalization;
using Ruth.Modules.Commerce.Contracts.Bookings;

namespace Ruth.Host.Components.Pages.Commerce;

internal static class CommerceBookingSpecFactory
{
    private static readonly DayOfWeek[] _weekdays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
    ];

    public static bool TryBuildServiceSpec(
        string name,
        string slug,
        BookingDeliveryMode deliveryMode,
        Guid revenueAccountId,
        string durationInput,
        string priceInput,
        string memberTierFreeInput,
        out BookableServiceSpec? spec
    )
    {
        spec = null;
        if (
            !int.TryParse(
                durationInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var duration
            )
            || !decimal.TryParse(
                priceInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var price
            )
        )
        {
            return false;
        }

        var freeTier = string.IsNullOrWhiteSpace(memberTierFreeInput)
            ? null
            : memberTierFreeInput.Trim();
        spec = new BookableServiceSpec(
            name,
            slug,
            deliveryMode,
            duration,
            SlotIncrementMinutes: 30,
            "USD",
            price,
            SetupBufferMinutes: 5,
            TeardownBufferMinutes: 10,
            IsFreeForMembers: freeTier is not null,
            FreeOrDiscountedMemberTier: freeTier,
            AllowPunchPassRedemption: true,
            RevenueAccountId: revenueAccountId,
            AllowInvoiceBilling: true
        );
        return true;
    }

    public static IReadOnlyList<CreateAvailabilityRuleCommand> BuildDefaultWeekdayRules(
        Guid serviceId
    )
    {
        return
        [
            .. _weekdays.Select(day => new CreateAvailabilityRuleCommand(
                new AvailabilityRuleSpec(
                    $"{day} Hours",
                    BookingTargetKind.Service,
                    AvailabilityRuleType.RecurringWindow,
                    BookableServiceId: serviceId,
                    DayOfWeek: day,
                    StartLocalTime: new TimeOnly(9, 0),
                    EndLocalTime: new TimeOnly(17, 0),
                    TimeZoneId: "UTC"
                )
            )),
        ];
    }
}
