using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Bookings;
using Ruth.Modules.Commerce.Slices.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class W4S10BookingsVerificationHelper
{
    public static async Task<ServiceBookingResponse> VerifySlotHoldsMemberFreeBookingOutboxAndWalletAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CommerceDbContext dbContext,
        CommerceSeedBundle seed,
        BookableServiceResponse service,
        DateTimeOffset slotStartUtc
    )
    {
        var slotsBeforeHold = (
            await queries.QueryAsync<
                QueryAvailableBookingSlotsQuery,
                IReadOnlyList<AvailableBookingSlotDto>
            >(
                new QueryAvailableBookingSlotsQuery(
                    BookingTargetKind.Service,
                    service.Slug,
                    slotStartUtc,
                    slotStartUtc.AddHours(4)
                )
            )
        ).Value;
        Assert.Equal(2, slotsBeforeHold.Count);

        var hold = (
            await commands.SendAsync<CreateBookingSlotHoldCommand, BookingSlotHoldResponse>(
                new CreateBookingSlotHoldCommand(
                    BookingTargetKind.Service,
                    service.Slug,
                    slotStartUtc
                )
            )
        ).Value;

        var conflictingHold = await commands.SendAsync<
            CreateBookingSlotHoldCommand,
            BookingSlotHoldResponse
        >(
            new CreateBookingSlotHoldCommand(
                BookingTargetKind.Service,
                service.Slug,
                slotStartUtc.AddHours(1)
            )
        );
        Assert.True(conflictingHold.IsFailure);

        var confirmed = (
            await commands.SendAsync<ConfirmBookingCommand, ServiceBookingResponse>(
                new ConfirmBookingCommand(
                    BookingTargetKind.Service,
                    service.Slug,
                    slotStartUtc,
                    "Nora Chen",
                    "nora.chen@example.org",
                    HoldToken: hold.HoldToken,
                    CustomerPersonId: seed.MemberPersonId
                )
            )
        ).Value;

        Assert.Equal(BookingPricingKind.MemberFree, confirmed.PricingKind);
        Assert.Equal(0m, confirmed.TotalAmount);
        Assert.True(confirmed.RequiresVirtualRoom);
        Assert.NotNull(confirmed.VirtualRoomReferenceKey);

        var hasOutboxEvent = await dbContext.OutboxMessages.AnyAsync(m =>
            m.EventType.Contains("ServiceBookingConfirmedIntegrationEvent")
        );
        Assert.True(hasOutboxEvent);

        var walletPasses = (
            await queries.QueryAsync<
                GetIssuedWalletPassesQuery,
                IReadOnlyList<IssuedWalletPassResponse>
            >(new GetIssuedWalletPassesQuery(PersonId: seed.MemberPersonId, ActiveOnly: true))
        ).Value;
        Assert.Contains(
            confirmed.ConfirmationCode,
            walletPasses[0].DynamicFields.NextShiftOrBookingSummary,
            StringComparison.Ordinal
        );

        return confirmed;
    }

    public static async Task VerifyResourceRentalPunchPassAndCancellationAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CommerceSeedBundle seed,
        BookableServiceResponse service,
        RentableResourceResponse resource,
        DateTimeOffset slotStartUtc,
        Guid serviceBookingId
    )
    {
        var rental = (
            await commands.SendAsync<ConfirmBookingCommand, ServiceBookingResponse>(
                new ConfirmBookingCommand(
                    BookingTargetKind.Resource,
                    resource.Slug,
                    slotStartUtc,
                    "Nora Chen",
                    "nora.chen@example.org",
                    RequestedDurationMinutes: 120,
                    CustomerPersonId: seed.MemberPersonId,
                    OneTimeHostedPaymentToken: "tok_rental_4242"
                )
            )
        ).Value;
        Assert.Equal(BookingPricingKind.MemberDiscounted, rental.PricingKind);
        Assert.Equal(80.00m, rental.TotalAmount);
        Assert.Equal(80.00m, rental.PaidAmount);

        var deniedCancel = await commands.SendAsync<CancelBookingCommand, ServiceBookingResponse>(
            new CancelBookingCommand(serviceBookingId, "Too close", StaffOverride: false)
        );
        Assert.True(deniedCancel.IsFailure);

        var staffCancel = await commands.SendAsync<CancelBookingCommand, ServiceBookingResponse>(
            new CancelBookingCommand(serviceBookingId, "Staff waiver", StaffOverride: true)
        );
        Assert.True(staffCancel.IsSuccess);

        var rentalCancel = await commands.SendAsync<CancelBookingCommand, ServiceBookingResponse>(
            new CancelBookingCommand(
                rental.Id,
                "Event moved",
                StaffOverride: true,
                IssueRefundOrVoidInvoice: true
            )
        );
        Assert.True(rentalCancel.IsSuccess);

        var passType = (
            await commands.SendAsync<CreatePunchPassTypeCommand, PunchPassTypeResponse>(
                new CreatePunchPassTypeCommand(
                    new PunchPassTypeSpec("Coaching 3-Pack", "COACH-3", 3)
                )
            )
        ).Value;
        var issuedPass = (
            await commands.SendAsync<IssuePunchPassCommand, IssuedPunchPassResponse>(
                new IssuePunchPassCommand(passType.Id, seed.HouseholdPartnerPersonId)
            )
        ).Value;

        var punchBooking = await commands.SendAsync<ConfirmBookingCommand, ServiceBookingResponse>(
            new ConfirmBookingCommand(
                BookingTargetKind.Service,
                service.Slug,
                slotStartUtc,
                "Liam Chen",
                "liam.chen@example.org",
                CustomerPersonId: seed.HouseholdPartnerPersonId,
                RedeemPunchPassId: issuedPass.Id
            )
        );
        Assert.True(punchBooking.IsSuccess);
        Assert.Equal(BookingPricingKind.PunchPassRedeemed, punchBooking.Value.PricingKind);

        var passesAfter = (
            await queries.QueryAsync<
                GetIssuedPunchPassesQuery,
                IReadOnlyList<IssuedPunchPassResponse>
            >(new GetIssuedPunchPassesQuery(OwnerPersonId: seed.HouseholdPartnerPersonId))
        ).Value;
        Assert.Equal(2, passesAfter[0].RemainingCredits);
    }

    public static async Task VerifyHoldCleanupJobAndPersonMergeAsync(
        IServiceProvider rootProvider,
        CommerceDbContext dbContext,
        CommerceSeedBundle seed,
        Guid cancelledBookingId,
        JobExecutionStatus expectedSucceededStatus
    )
    {
        var job = new ExpiredBookingSlotHoldCleanupJob(
            rootProvider.GetRequiredService<IServiceScopeFactory>()
        );
        var jobRes = await job.ExecuteAsync(CancellationToken.None);
        Assert.Equal(expectedSucceededStatus, jobRes.Status);

        var consumer = new CommercePersonMergedConsumer(dbContext);
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                SurvivingPersonId: seed.MemberPersonId,
                RetiredPersonId: seed.HouseholdPartnerPersonId,
                [],
                HadMultipleDistinctEmails: false
            ),
            CancellationToken.None
        );

        var cancelledId = ServiceBookingId.From(cancelledBookingId);
        var repointedCount = await dbContext.ServiceBookings.CountAsync(b =>
            b.Id != cancelledId && b.CustomerPersonId == seed.MemberPersonId
        );
        Assert.True(repointedCount >= 1);
    }
}
