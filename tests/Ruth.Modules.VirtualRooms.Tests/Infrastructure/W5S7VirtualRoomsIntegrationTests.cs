using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Contracts.Webhooks;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.Modules.VirtualRooms.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.VirtualRooms.Tests.Infrastructure;

[Collection(VirtualRoomsPostgresFixtures.Name)]
public sealed class W5S7VirtualRoomsIntegrationTests(VirtualRoomsPostgresContainerFixture postgres)
{
    private const string TestApiKey = "devkey";
    private const string TestApiSecret = "ruth-inmemory-livekit-dev-signing-secret-32chars!";

    [Fact]
    public async Task AclJoinTokens_LiveKitWebhooks_AutoGroupAttendance_AndRustFsTranscriptManifest_WorkEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        var storageSpy = new VirtualRoomsRecordingStorageSpy();
        await using var provider = await VirtualRoomsTestHostFactory.BuildAndMigrateAsync(
            conn,
            storageSpy
        );

        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var commands = sp.GetRequiredService<ICommandDispatcher>();
        var queries = sp.GetRequiredService<IQueryDispatcher>();
        var vrDb = sp.GetRequiredService<VirtualRoomsDbContext>();

        var (groupId, hostPersonId, attendeePersonId) =
            await VirtualRoomsTestSeedHelper.SeedGroupAndPeopleAsync(commands);
        var room = await VirtualRoomsTestSeedHelper.CreateGroupVirtualRoomAsync(
            commands,
            groupId,
            hostPersonId
        );

        await VirtualRoomsTestSeedHelper.VerifyAclTokenMintingAsync(
            commands,
            room,
            hostPersonId,
            attendeePersonId
        );

        var session = await StartLiveSessionAndVerifyAttendanceWebhooksAsync(
            commands,
            queries,
            room,
            groupId,
            attendeePersonId
        );
        await VerifyRecordingEgressAndOutboxAsync(commands, vrDb, storageSpy, room, session.Id);
    }

    private static async Task<VirtualRoomSessionResponse> StartLiveSessionAndVerifyAttendanceWebhooksAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        VirtualRoomResponse room,
        Guid groupId,
        Guid attendeePersonId
    )
    {
        var session = await commands.SendAsync<
            CreateVirtualRoomSessionCommand,
            VirtualRoomSessionResponse
        >(
            new CreateVirtualRoomSessionCommand(
                room.Id,
                "Weekly Cohort Live Session",
                new DateTimeOffset(2026, 11, 25, 18, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero),
                StartLiveNow: true
            )
        );
        Assert.True(session.IsSuccess);

        var unsignedReject = await commands.SendAsync<
            LiveKitWebhookIngestCommand,
            LiveKitWebhookIngestResponse
        >(
            new LiveKitWebhookIngestCommand(
                "Bearer invalid",
                JsonSerializer.Serialize(new { @event = "room_started" })
            )
        );
        Assert.True(unsignedReject.IsFailure);

        var baseUnix = new DateTimeOffset(
            2026,
            11,
            25,
            18,
            0,
            0,
            TimeSpan.Zero
        ).ToUnixTimeSeconds();
        _ = await SendParticipantEventAsync(
            commands,
            "participant_joined",
            baseUnix,
            room.LiveKitRoomName,
            attendeePersonId
        );
        var shortLeave = await SendParticipantEventAsync(
            commands,
            "participant_left",
            baseUnix + 120,
            room.LiveKitRoomName,
            attendeePersonId
        );
        Assert.False(shortLeave.AttendanceRecorded);

        _ = await SendParticipantEventAsync(
            commands,
            "participant_joined",
            baseUnix + 200,
            room.LiveKitRoomName,
            attendeePersonId
        );
        var qualifyingLeave = await SendParticipantEventAsync(
            commands,
            "participant_left",
            baseUnix + 420,
            room.LiveKitRoomName,
            attendeePersonId
        );
        Assert.True(qualifyingLeave.AttendanceRecorded);

        var groupOccurrences = await queries.QueryAsync<
            GetGroupAttendanceOccurrencesQuery,
            IReadOnlyList<AttendanceOccurrenceSummaryResponse>
        >(new GetGroupAttendanceOccurrencesQuery(groupId));
        var occ = Assert.Single(groupOccurrences.Value);
        Assert.Equal(1, occ.PresentCount);

        return session.Value;
    }

    private static async Task VerifyRecordingEgressAndOutboxAsync(
        ICommandDispatcher commands,
        VirtualRoomsDbContext vrDb,
        VirtualRoomsRecordingStorageSpy storageSpy,
        VirtualRoomResponse room,
        Guid sessionId
    )
    {
        var recStart = await commands.SendAsync<
            StartVirtualRoomRecordingCommand,
            VirtualRoomRecordingArchiveResponse
        >(new StartVirtualRoomRecordingCommand(sessionId));
        Assert.True(recStart.IsSuccess);

        var egressEnded = await SendSignedWebhookAsync(
            commands,
            JsonSerializer.Serialize(
                new
                {
                    @event = "egress_ended",
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    egressInfo = new
                    {
                        egressId = recStart.Value.EgressId,
                        roomName = room.LiveKitRoomName,
                        fileResults = new object[]
                        {
                            new
                            {
                                filename = recStart.Value.StorageObjectKey,
                                size = 2097152L,
                                duration = 600000000000L,
                            },
                        },
                    },
                }
            )
        );
        Assert.True(egressEnded.RecordingArchived);
        Assert.Contains(storageSpy.UploadedUtf8Payloads.Keys, static k =>
            k.EndsWith(".transcript.json", StringComparison.Ordinal)
        );

        var outboxTypes = await vrDb
            .OutboxMessages.AsNoTracking()
            .Select(static o => o.EventType)
            .ToListAsync();
        Assert.Contains(outboxTypes, static t =>
            t.Contains("VirtualRoomAttendanceQualifiedIntegrationEvent", StringComparison.Ordinal)
        );
        Assert.Contains(outboxTypes, static t =>
            t.Contains("VirtualRoomRecordingArchivedIntegrationEvent", StringComparison.Ordinal)
        );
    }

    private static Task<LiveKitWebhookIngestResponse> SendParticipantEventAsync(
        ICommandDispatcher commands,
        string eventType,
        long unixSeconds,
        string roomName,
        Guid personId
    )
    {
        return SendSignedWebhookAsync(
            commands,
            JsonSerializer.Serialize(
                new
                {
                    @event = eventType,
                    createdAt = unixSeconds,
                    room = new { name = roomName },
                    participant = new { identity = $"person:{personId:D}", name = "Attendee One" },
                }
            )
        );
    }

    private static async Task<LiveKitWebhookIngestResponse> SendSignedWebhookAsync(
        ICommandDispatcher commands,
        string rawJson
    )
    {
        var jwt = LiveKitJwtAuthenticator.SignWebhookPayload(
            TestApiKey,
            TestApiSecret,
            Encoding.UTF8.GetBytes(rawJson),
            DateTimeOffset.UtcNow
        );
        var res = await commands.SendAsync<
            LiveKitWebhookIngestCommand,
            LiveKitWebhookIngestResponse
        >(new LiveKitWebhookIngestCommand($"Bearer {jwt}", rawJson));
        Assert.True(res.IsSuccess);
        return res.Value;
    }
}
