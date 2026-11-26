using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Domain;

public sealed class W4S10DomainInvariantsTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 12, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AvailabilityRule_ExpandsWeeklyRecurringAndBlackoutWindows_ValidatesSpecs_AndRepointsProvider()
    {
        var svcId = Guid.CreateVersion7();
        var oldProvider = Guid.CreateVersion7();
        var newProvider = Guid.CreateVersion7();

        Assert.True(
            AvailabilityRule
                .Create(
                    new AvailabilityRuleSpec(
                        "",
                        BookingTargetKind.Service,
                        AvailabilityRuleType.RecurringWindow,
                        BookableServiceId: svcId
                    )
                )
                .IsFailure
        );
        Assert.True(
            AvailabilityRule
                .Create(
                    new AvailabilityRuleSpec(
                        "Missing Target",
                        BookingTargetKind.Resource,
                        AvailabilityRuleType.CustomDateWindow
                    )
                )
                .IsFailure
        );

        var weekly = AvailabilityRule
            .Create(
                new AvailabilityRuleSpec(
                    "Wednesday Morning Clinic",
                    BookingTargetKind.Service,
                    AvailabilityRuleType.RecurringWindow,
                    BookableServiceId: svcId,
                    ProviderPersonId: oldProvider,
                    DayOfWeek: DayOfWeek.Wednesday,
                    StartLocalTime: new TimeOnly(9, 0),
                    EndLocalTime: new TimeOnly(12, 0),
                    TimeZoneId: "UTC"
                )
            )
            .Value;

        var expanded = weekly.ExpandDirectWindows(_nowUtc.AddHours(-3), _nowUtc.AddDays(8));
        Assert.Equal(2, expanded.Count);
        Assert.Empty(weekly.ExpandDirectWindows(_nowUtc, _nowUtc.AddHours(-1)));

        Assert.False(weekly.RepointPerson(Guid.CreateVersion7(), newProvider));
        Assert.True(weekly.RepointPerson(oldProvider, newProvider));
        Assert.Equal(newProvider, weekly.ProviderPersonId);

        var blackout = AvailabilityRule
            .Create(
                new AvailabilityRuleSpec(
                    "Holiday Maintenance",
                    BookingTargetKind.Service,
                    AvailabilityRuleType.BlackoutWindow,
                    BookableServiceId: svcId,
                    CustomStartUtc: _nowUtc,
                    CustomEndUtc: _nowUtc.AddHours(2)
                )
            )
            .Value;
        _ = Assert.Single(blackout.ExpandDirectWindows(_nowUtc.AddHours(-1), _nowUtc.AddHours(3)));
    }

    [Fact]
    public void BookingSlotHold_And_ResourceQuote_HandleGroupEligibility_Completion_AndRepoint()
    {
        var groupA = Guid.CreateVersion7();
        var locId = Guid.CreateVersion7();
        var revAcc = Guid.CreateVersion7();
        var oldProvider = Guid.CreateVersion7();
        var newProvider = Guid.CreateVersion7();

        var resource = RentableResource
            .Create(
                new RentableResourceSpec(
                    "Community Hall",
                    "community-hall",
                    RentableResourceKind.Room,
                    locId,
                    60,
                    240,
                    60,
                    "USD",
                    50m,
                    FlatCleaningOrSetupFee: 20m,
                    IsFreeForMembers: true,
                    FreeOrDiscountedMemberGroupId: groupA,
                    RevenueAccountId: revAcc
                )
            )
            .Value;

        var freeQuote = BookingPricingCalculator.EvaluateResourceQuote(
            resource,
            120,
            1,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid> { groupA }
        );
        Assert.Equal(BookingPricingKind.MemberFree, freeQuote.PricingKind);
        Assert.Equal(0m, freeQuote.TotalAmount);

        var paidQuote = BookingPricingCalculator.EvaluateResourceQuote(
            resource,
            120,
            1,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid>()
        );
        Assert.Equal(BookingPricingKind.StandardPaid, paidQuote.PricingKind);
        Assert.Equal(120m, paidQuote.TotalAmount);

        var generalMemberResource = RentableResource
            .Create(
                new RentableResourceSpec(
                    "Open Lounge",
                    "open-lounge",
                    RentableResourceKind.Room,
                    locId,
                    60,
                    120,
                    60,
                    "USD",
                    25m,
                    IsFreeForMembers: false,
                    RevenueAccountId: revAcc
                )
            )
            .Value;
        Assert.Equal(
            BookingPricingKind.StandardPaid,
            BookingPricingCalculator
                .EvaluateResourceQuote(
                    generalMemberResource,
                    60,
                    1,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Gold" },
                    new HashSet<Guid>()
                )
                .PricingKind
        );

        var anyMemberFreeResource = RentableResource
            .Create(
                new RentableResourceSpec(
                    "Member Study Room",
                    "member-study-room",
                    RentableResourceKind.Room,
                    locId,
                    60,
                    120,
                    60,
                    "USD",
                    25m,
                    IsFreeForMembers: true,
                    RevenueAccountId: revAcc
                )
            )
            .Value;
        Assert.Equal(
            BookingPricingKind.MemberFree,
            BookingPricingCalculator
                .EvaluateResourceQuote(
                    anyMemberFreeResource,
                    60,
                    1,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Silver" },
                    new HashSet<Guid>()
                )
                .PricingKind
        );

        var slot = new AvailableBookingSlotDto(
            _nowUtc,
            _nowUtc.AddHours(2),
            _nowUtc,
            _nowUtc.AddHours(2),
            oldProvider,
            locId,
            1
        );
        var hold = BookingSlotHold.Create(
            BookingTargetKind.Resource,
            serviceId: null,
            resource.Id,
            oldProvider,
            locId,
            slot,
            1,
            15,
            _nowUtc
        );
        Assert.True(hold.IsActiveAt(_nowUtc.AddMinutes(5)));
        Assert.True(
            hold.MatchesCandidate(
                BookingTargetKind.Resource,
                serviceId: null,
                resource.Id,
                _nowUtc,
                _nowUtc.AddHours(2),
                1
            )
        );
        Assert.True(hold.RepointPerson(oldProvider, newProvider));
        hold.MarkCompleted(ServiceBookingId.Create());
        Assert.False(hold.IsActiveAt(_nowUtc.AddMinutes(5)));
    }
}
