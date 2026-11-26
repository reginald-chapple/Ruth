using System.Globalization;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class W4S10BookingsSetupHelper
{
    public static async Task<(
        BookableServiceResponse Service,
        RentableResourceResponse Resource,
        DateTimeOffset SlotStartUtc
    )> SetupServiceResourceAndSchedulesAsync(ICommandDispatcher commands, CommerceSeedBundle seed)
    {
        var providerPerson = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Coach", "Rivera", Email: "coach.rivera@example.org")
            )
        ).Value;

        var targetDay = DateTimeOffset.UtcNow.AddDays(3);
        var windowStart = new DateTimeOffset(
            targetDay.Year,
            targetDay.Month,
            targetDay.Day,
            13,
            0,
            0,
            TimeSpan.Zero
        );

        var coreScheduleId = await CreateCoreScheduleAsync(commands, windowStart);
        var service = await CreateMentorshipServiceAsync(
            commands,
            seed,
            providerPerson.Id,
            coreScheduleId,
            windowStart
        );
        var resource = await CreateStudioResourceAsync(commands, seed, windowStart);

        return (service, resource, windowStart);
    }

    private static async Task<Guid> CreateCoreScheduleAsync(
        ICommandDispatcher commands,
        DateTimeOffset windowStart
    )
    {
        var iCal = string.Create(
            CultureInfo.InvariantCulture,
            $"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nDTSTART:{windowStart:yyyyMMdd'T'HHmmss'Z'}\r\nDTEND:{windowStart.AddHours(4):yyyyMMdd'T'HHmmss'Z'}\r\nEND:VEVENT\r\nEND:VCALENDAR"
        );
        var coreSchedule = (
            await commands.SendAsync<CreateScheduleCommand, ScheduleResponse>(
                new CreateScheduleCommand(
                    Name: "Coaching Core Schedule",
                    ICalendarContent: iCal,
                    TimeZoneId: "UTC"
                )
            )
        ).Value;
        return coreSchedule.Id;
    }

    private static async Task<BookableServiceResponse> CreateMentorshipServiceAsync(
        ICommandDispatcher commands,
        CommerceSeedBundle seed,
        Guid providerPersonId,
        Guid coreScheduleId,
        DateTimeOffset windowStart
    )
    {
        var service = (
            await commands.SendAsync<CreateBookableServiceCommand, BookableServiceResponse>(
                new CreateBookableServiceCommand(
                    new BookableServiceSpec(
                        "1-on-1 Virtual Mentorship",
                        "virtual-mentorship-1on1",
                        BookingDeliveryMode.VirtualRoom,
                        DurationMinutes: 60,
                        SlotIncrementMinutes: 60,
                        "USD",
                        75m,
                        SetupBufferMinutes: 15,
                        TeardownBufferMinutes: 15,
                        CancellationNoticeMinutes: 10080,
                        IsFreeForMembers: true,
                        FreeOrDiscountedMemberTier: "Gold",
                        AllowPunchPassRedemption: true,
                        PunchPassCreditsRequired: 1,
                        RevenueAccountId: seed.OperatingAccountId,
                        DefaultFinancialGatewayId: seed.GatewayId,
                        AssignedProviderPersonIds: [providerPersonId]
                    )
                )
            )
        ).Value;

        _ = await commands.SendAsync<CreateAvailabilityRuleCommand, AvailabilityRuleResponse>(
            new CreateAvailabilityRuleCommand(
                new AvailabilityRuleSpec(
                    "Core Schedule Window",
                    BookingTargetKind.Service,
                    AvailabilityRuleType.RecurringWindow,
                    BookableServiceId: service.Id,
                    ProviderPersonId: providerPersonId,
                    ScheduleId: coreScheduleId
                )
            )
        );
        _ = await commands.SendAsync<CreateAvailabilityRuleCommand, AvailabilityRuleResponse>(
            new CreateAvailabilityRuleCommand(
                new AvailabilityRuleSpec(
                    "Late Afternoon Blackout",
                    BookingTargetKind.Service,
                    AvailabilityRuleType.BlackoutWindow,
                    BookableServiceId: service.Id,
                    ProviderPersonId: providerPersonId,
                    CustomStartUtc: windowStart.AddHours(3),
                    CustomEndUtc: windowStart.AddHours(4)
                )
            )
        );
        return service;
    }

    private static async Task<RentableResourceResponse> CreateStudioResourceAsync(
        ICommandDispatcher commands,
        CommerceSeedBundle seed,
        DateTimeOffset windowStart
    )
    {
        var resource = (
            await commands.SendAsync<CreateRentableResourceCommand, RentableResourceResponse>(
                new CreateRentableResourceCommand(
                    new RentableResourceSpec(
                        "Courtroom & Studio B",
                        "courtroom-studio-b",
                        RentableResourceKind.Studio,
                        seed.LocationId,
                        60,
                        180,
                        60,
                        "USD",
                        HourlyRate: 60m,
                        FlatCleaningOrSetupFee: 20m,
                        MemberHourlyRate: 30m,
                        FreeOrDiscountedMemberTier: "Gold",
                        RevenueAccountId: seed.OperatingAccountId,
                        DefaultFinancialGatewayId: seed.GatewayId,
                        AllowInvoiceBilling: true
                    )
                )
            )
        ).Value;

        _ = await commands.SendAsync<CreateAvailabilityRuleCommand, AvailabilityRuleResponse>(
            new CreateAvailabilityRuleCommand(
                new AvailabilityRuleSpec(
                    "Studio Window",
                    BookingTargetKind.Resource,
                    AvailabilityRuleType.CustomDateWindow,
                    RentableResourceId: resource.Id,
                    CustomStartUtc: windowStart,
                    CustomEndUtc: windowStart.AddHours(4)
                )
            )
        );
        return resource;
    }
}
