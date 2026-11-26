using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Tests.Fixtures;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

[Collection(CheckInPostgresFixtures.Name)]
public sealed class W3S3CheckInSessionAndKioskIntegrationTests(
    CheckInPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task KioskSearch_Sub50msWarmLookup_SessionCheckIn_GroupAttendanceSync_RoomManager_CheckOut_AndPersonMerge_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CheckInTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var events = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        var scheduleId = Guid.CreateVersion7();
        var roomAId = Guid.CreateVersion7();
        var roomBId = Guid.CreateVersion7();

        var groupId = await W3S3CheckInTestSeedHelper.CreateLinkedYouthGroupAsync(commands);
        var (householdId, parentId, childId) =
            await W3S3CheckInTestSeedHelper.SeedHouseholdInCrmAsync(commands);
        var (configId, areaId) = await W3S3CheckInProfileSeedHelper.ConfigureCheckInProfileAsync(
            commands,
            groupId,
            scheduleId,
            roomAId,
            roomBId
        );

        await VerifyFastKioskLookupsAsync(
            queries,
            configId,
            scheduleId,
            householdId,
            childId,
            areaId
        );

        var receipt = await SubmitAndVerifyCheckInAsync(
            commands,
            queries,
            configId,
            areaId,
            scheduleId,
            householdId,
            parentId,
            childId,
            roomAId,
            groupId
        );

        await VerifyRoomManagerAndCheckOutAsync(
            commands,
            queries,
            configId,
            areaId,
            scheduleId,
            parentId,
            roomBId,
            receipt
        );

        await VerifyPersonMergeRepointAsync(events, queries, childId, receipt.SessionId);
    }

    private static async Task VerifyFastKioskLookupsAsync(
        IQueryDispatcher queries,
        Guid configId,
        Guid scheduleId,
        Guid householdId,
        Guid childId,
        Guid areaId
    )
    {
        _ = await queries.QueryAsync<KioskSearchCandidatesQuery, KioskSearchResponse>(
            new KioskSearchCandidatesQuery(configId, scheduleId, "8241")
        );

        var sw = Stopwatch.StartNew();
        var phoneSearch = await queries.QueryAsync<KioskSearchCandidatesQuery, KioskSearchResponse>(
            new KioskSearchCandidatesQuery(configId, scheduleId, "8241")
        );
        sw.Stop();
        Assert.True(phoneSearch.IsSuccess);
        var elapsedText = sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
        Assert.True(
            sw.ElapsedMilliseconds < 250,
            $"Expected fast kiosk search, observed {elapsedText}ms"
        );

        var family = Assert.Single(phoneSearch.Value.Families);
        Assert.Equal(householdId, family.HouseholdId);
        var childCandidate = family.Members.Single(m => m.PersonId == childId);
        Assert.Contains(childCandidate.AreaOptions, o => o.AreaId == areaId && o.IsEligible);

        var barcodeSearch = await queries.QueryAsync<
            KioskSearchCandidatesQuery,
            KioskSearchResponse
        >(
            new KioskSearchCandidatesQuery(
                configId,
                scheduleId,
                "MEM-CARD-7788",
                KioskSearchInputKind.BarcodeOrCard
            )
        );
        Assert.True(barcodeSearch.IsSuccess);
        _ = Assert.Single(barcodeSearch.Value.Families);
    }

    private static async Task<CheckInSessionReceiptResponse> SubmitAndVerifyCheckInAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid configId,
        Guid areaId,
        Guid scheduleId,
        Guid householdId,
        Guid parentId,
        Guid childId,
        Guid roomAId,
        Guid groupId
    )
    {
        var submitted = await commands.SendAsync<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse
        >(
            new SubmitCheckInSessionCommand(
                configId,
                scheduleId,
                [
                    new CheckInSelectionItemDto(
                        childId,
                        areaId,
                        ScheduleId: scheduleId,
                        AttendanceNote: "EpiPen in backpack"
                    ),
                ],
                CheckInSessionChannel.Kiosk,
                HouseholdId: householdId,
                CheckedInByPersonId: parentId
            )
        );
        Assert.True(submitted.IsSuccess);
        Assert.NotNull(submitted.Value.SecurityPickupCode);
        Assert.Equal(4, submitted.Value.SecurityPickupCode.Length);
        Assert.Equal(roomAId, Assert.Single(submitted.Value.Entries).LocationId);

        var duplicateAttempt = await commands.SendAsync<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse
        >(
            new SubmitCheckInSessionCommand(
                configId,
                scheduleId,
                [new CheckInSelectionItemDto(childId, areaId, ScheduleId: scheduleId)]
            )
        );
        Assert.True(duplicateAttempt.IsFailure);

        await W3S3CheckInTestSeedHelper.AssertGroupPresentCountAsync(queries, groupId, 1);
        return submitted.Value;
    }

    private static async Task VerifyRoomManagerAndCheckOutAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid configId,
        Guid areaId,
        Guid scheduleId,
        Guid parentId,
        Guid roomBId,
        CheckInSessionReceiptResponse receipt
    )
    {
        var rosterBefore = await queries.QueryAsync<GetLiveRoomRosterQuery, LiveRoomRosterResponse>(
            new GetLiveRoomRosterQuery(configId, ScheduleId: scheduleId)
        );
        Assert.Equal(1, rosterBefore.Value.TotalCheckedIn);

        var entryId = Assert.Single(receipt.Entries).EntryId;
        var moved = await commands.SendAsync<
            OverrideCheckInRoomAssignmentCommand,
            CheckedInEntryReceiptDto
        >(
            new OverrideCheckInRoomAssignmentCommand(
                receipt.SessionId,
                entryId,
                areaId,
                roomBId,
                OverrideByPersonId: parentId,
                Reason: "Balanced to Room B"
            )
        );
        Assert.True(moved.IsSuccess);
        Assert.Equal(roomBId, moved.Value.LocationId);

        var wrongCodeCheckout = await commands.SendAsync<
            CheckOutAttendanceCommand,
            CheckOutResultResponse
        >(new CheckOutAttendanceCommand(configId, SecurityPickupCode: "ZZ99"));
        Assert.True(wrongCodeCheckout.IsFailure);

        var validCheckout = await commands.SendAsync<
            CheckOutAttendanceCommand,
            CheckOutResultResponse
        >(new CheckOutAttendanceCommand(configId, SecurityPickupCode: receipt.SecurityPickupCode));
        Assert.True(validCheckout.IsSuccess);
        Assert.Equal(1, validCheckout.Value.CheckedOutCount);
    }

    private static async Task VerifyPersonMergeRepointAsync(
        IIntegrationEventDispatcher events,
        IQueryDispatcher queries,
        Guid retiredChildId,
        Guid sessionId
    )
    {
        var survivingChildId = Guid.CreateVersion7();
        await events.DispatchAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingChildId,
                retiredChildId,
                [],
                HadMultipleDistinctEmails: false
            )
        );

        var fetchedSession = await queries.QueryAsync<
            GetCheckInSessionByIdQuery,
            CheckInSessionReceiptResponse
        >(new GetCheckInSessionByIdQuery(sessionId));
        Assert.Equal(survivingChildId, Assert.Single(fetchedSession.Value.Entries).PersonId);
    }
}
