using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Domain;

public sealed class W4S10BookingSlotEngineAndPricingDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 12, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BookingSlotEngine_HonorsSetupTeardownBuffers_LeadTime_Blackouts_AndWorkshopMultiSeatCapacity()
    {
        var serviceId = BookableServiceId.Create();
        var providerA = Guid.CreateVersion7();
        var locationA = Guid.CreateVersion7();

        var oneOnOneParams = new SlotEngineParameters(
            BookingTargetKind.Service,
            serviceId,
            RentableResourceId: null,
            locationA,
            DurationMinutes: 60,
            SlotIncrementMinutes: 30,
            SetupBufferMinutes: 15,
            TeardownBufferMinutes: 15,
            CapacityPerSlot: 1,
            MinimumLeadTimeMinutes: 60,
            MaximumAdvanceDays: 14,
            RequestedQuantity: 1,
            RequestedProviderPersonId: providerA,
            CandidateProviderIds: [providerA]
        );

        var windowStart = new DateTimeOffset(2026, 12, 1, 8, 30, 0, TimeSpan.Zero);
        var windowEnd = new DateTimeOffset(2026, 12, 1, 13, 0, 0, TimeSpan.Zero);
        NormalizedAvailabilityInterval[] open = [new(windowStart, windowEnd, providerA, locationA)];

        NormalizedAvailabilityInterval[] blackouts =
        [
            new(
                new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 1, 13, 0, 0, TimeSpan.Zero),
                providerA,
                LocationId: null
            ),
        ];

        OccupiedBookingBlock[] occupied =
        [
            new(
                BookingTargetKind.Service,
                serviceId,
                RentableResourceId: null,
                providerA,
                locationA,
                new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 1, 11, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 1, 9, 45, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 12, 1, 11, 15, 0, TimeSpan.Zero),
                1
            ),
        ];

        var slots = BookingSlotEngine.ComputeAvailableSlots(
            oneOnOneParams,
            open,
            blackouts,
            occupied,
            windowStart,
            windowEnd,
            _nowUtc
        );
        Assert.Empty(slots);

        var slotsWithoutBlackout = BookingSlotEngine.ComputeAvailableSlots(
            oneOnOneParams,
            open,
            [],
            occupied,
            windowStart,
            windowEnd,
            _nowUtc
        );
        Assert.Equal(2, slotsWithoutBlackout.Count);
        Assert.Equal(
            new DateTimeOffset(2026, 12, 1, 11, 30, 0, TimeSpan.Zero),
            slotsWithoutBlackout[0].SlotStartUtc
        );
        Assert.Equal(
            new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero),
            slotsWithoutBlackout[1].SlotStartUtc
        );

        var workshopParams = oneOnOneParams with { CapacityPerSlot = 5, RequestedQuantity = 2 };
        var workshopExact = BookingSlotEngine.ValidateExactSlot(
            workshopParams,
            new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero),
            open,
            [],
            occupied,
            _nowUtc
        );
        Assert.NotNull(workshopExact);
        Assert.Equal(4, workshopExact.RemainingCapacity);
    }

    [Fact]
    public void BookingPricingCalculator_And_BookingCancellationPolicy_EnforceMemberAndNoticeRules()
    {
        var revAcc = Guid.CreateVersion7();
        var service = BookableService
            .Create(
                new BookableServiceSpec(
                    "Virtual Financial Intake",
                    "virtual-financial-intake",
                    BookingDeliveryMode.VirtualRoom,
                    45,
                    30,
                    "USD",
                    80m,
                    CancellationNoticeMinutes: 1440,
                    IsFreeForMembers: false,
                    MemberOverridePrice: 30m,
                    FreeOrDiscountedMemberTier: "Silver",
                    AllowPunchPassRedemption: true,
                    PunchPassCreditsRequired: 2,
                    RevenueAccountId: revAcc
                )
            )
            .Value;

        var stdQuote = BookingPricingCalculator.EvaluateServiceQuote(
            service,
            1,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid>(),
            redeemPunchPassRequested: false
        );
        Assert.Equal(BookingPricingKind.StandardPaid, stdQuote.PricingKind);
        Assert.Equal(80m, stdQuote.TotalAmount);

        var memberQuote = BookingPricingCalculator.EvaluateServiceQuote(
            service,
            2,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Silver" },
            new HashSet<Guid>(),
            redeemPunchPassRequested: false
        );
        Assert.Equal(BookingPricingKind.MemberDiscounted, memberQuote.PricingKind);
        Assert.Equal(60m, memberQuote.TotalAmount);
        Assert.Equal(100m, memberQuote.DiscountAmount);

        var punchQuote = BookingPricingCalculator.EvaluateServiceQuote(
            service,
            1,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid>(),
            redeemPunchPassRequested: true
        );
        Assert.Equal(BookingPricingKind.PunchPassRedeemed, punchQuote.PricingKind);
        Assert.Equal(0m, punchQuote.TotalAmount);
        Assert.Equal(2, punchQuote.PunchPassCreditsRequired);

        var slot = new AvailableBookingSlotDto(
            _nowUtc.AddHours(6),
            _nowUtc.AddHours(6).AddMinutes(45),
            _nowUtc.AddHours(6),
            _nowUtc.AddHours(6).AddMinutes(45),
            ProviderPersonId: null,
            LocationId: null,
            1
        );
        var booking = ServiceOrResourceBooking
            .Create(
                BookingTargetKind.Service,
                service.Id,
                resourceId: null,
                service.Name,
                service.DeliveryMode,
                branchId: null,
                slot,
                1,
                Guid.CreateVersion7(),
                "Riley Customer",
                "riley@example.org",
                customerPhone: null,
                customerNotes: null,
                stdQuote,
                _nowUtc
            )
            .Value;

        Assert.True(booking.RequiresVirtualRoom);
        Assert.NotNull(booking.VirtualRoomReferenceKey);

        Assert.True(booking.Cancel(1440, staffOverride: false, "Too late", _nowUtc).IsFailure);
        Assert.True(
            booking.Cancel(1440, staffOverride: true, "Coordinator waiver", _nowUtc).IsSuccess
        );
        Assert.Equal(ServiceBookingStatus.Cancelled, booking.Status);
    }
}
