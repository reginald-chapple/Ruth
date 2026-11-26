using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.VirtualRooms.Tests.Infrastructure;

[Collection(VirtualRoomsPostgresFixtures.Name)]
public sealed class W5S7VirtualRoomsConsumersIntegrationTests(
    VirtualRoomsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task BookingConfirmedConsumer_AndPersonMergeConsumer_ProvisionAndMergeIdempotently()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await VirtualRoomsTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var queries = sp.GetRequiredService<IQueryDispatcher>();

        var bookingId = Guid.CreateVersion7();
        var retiredCustomerId = Guid.CreateVersion7();
        var survivingCustomerId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;

        var bookingConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<ServiceBookingConfirmedIntegrationEvent>
        >();
        var bookingEvt = new ServiceBookingConfirmedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOnUtc: now,
            BookingId: bookingId,
            ConfirmationCode: "BK-VR-1",
            TargetKind: BookingTargetKind.Service,
            BookableServiceId: Guid.CreateVersion7(),
            RentableResourceId: null,
            TargetName: "1-on-1 Advisory Session",
            DeliveryMode: BookingDeliveryMode.VirtualRoom,
            RequiresVirtualRoom: true,
            VirtualRoomReferenceKey: "bk-vr-1",
            CustomerPersonId: retiredCustomerId,
            CustomerName: "Taylor Client",
            CustomerEmail: "taylor@example.org",
            ProviderPersonId: null,
            LocationId: null,
            BranchId: null,
            StartUtc: now.AddHours(2),
            EndUtc: now.AddHours(3),
            ParticipantOrUnitCount: 1,
            CurrencyCode: "USD",
            TotalAmount: 95m,
            PaidAmount: 95m,
            LinkedInvoiceId: null
        );
        await bookingConsumer.ConsumeAsync(bookingEvt);
        await bookingConsumer.ConsumeAsync(bookingEvt);

        var rooms = await queries.QueryAsync<
            GetVirtualRoomsQuery,
            IReadOnlyList<VirtualRoomResponse>
        >(new GetVirtualRoomsQuery(ContextKind: VirtualRoomContextKind.CommerceBooking));
        var bookingRoom = Assert.Single(rooms.Value);
        Assert.Equal(bookingId, bookingRoom.LinkedBookingId);

        var mergeConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await mergeConsumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                survivingCustomerId,
                retiredCustomerId,
                [],
                HadMultipleDistinctEmails: false
            )
        );
    }
}
