using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Tests.Fixtures;
using Ruth.Providers.LiveKit;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Modules.VirtualRooms.Tests.Domain;

public sealed class W5S7VirtualRoomsDomainAndJwtTests
{
    [Fact]
    public void VirtualRoom_AndSession_MonotonicDwell_Qualification_Recordings_AndPersonMerge_WorkDeterministically()
    {
        var hostId = Guid.CreateVersion7();
        var survivingId = Guid.CreateVersion7();
        var retiredId = Guid.CreateVersion7();
        var t0 = new DateTimeOffset(2026, 11, 25, 20, 0, 0, TimeSpan.Zero);

        Assert.True(
            VirtualRoom
                .Create(new VirtualRoomSpec("", "bad slug!", MinimumAttendanceDwellSeconds: -1))
                .IsFailure
        );

        var room = VirtualRoom
            .Create(
                new VirtualRoomSpec(
                    "Leadership Cohort Room",
                    "leadership-cohort-room",
                    VirtualRoomContextKind.GroupMeeting,
                    MinimumAttendanceDwellSeconds: 300,
                    HostModeratorPersonId: retiredId
                )
            )
            .Value;
        Assert.True(room.RepointPerson(retiredId, hostId));

        var session = VirtualRoomSession.Create(room, "Week 1 Session", t0, t0.AddHours(1)).Value;
        _ = session.RecordParticipantJoined(
            $"person:{survivingId:D}",
            survivingId,
            "Alex",
            VirtualParticipantRole.Participant,
            t0
        );
        _ = session.RecordParticipantLeft(
            $"person:{survivingId:D}",
            survivingId,
            room.MinimumAttendanceDwellSeconds,
            t0.AddSeconds(180),
            out var qualifiedFirst
        );
        Assert.False(qualifiedFirst);
        Assert.Equal(180, session.Participants[0].CumulativeDwellSeconds);

        _ = session.RecordParticipantJoined(
            $"person:{survivingId:D}",
            survivingId,
            "Alex",
            VirtualParticipantRole.Participant,
            t0.AddSeconds(240)
        );
        _ = session.RecordParticipantLeft(
            $"person:{survivingId:D}",
            survivingId,
            room.MinimumAttendanceDwellSeconds,
            t0.AddSeconds(400),
            out var qualifiedSecond
        );
        Assert.True(qualifiedSecond);
        Assert.Equal(340, session.Participants[0].CumulativeDwellSeconds);

        _ = session.RecordParticipantJoined(
            $"person:{retiredId:D}",
            retiredId,
            "Alex Alt",
            VirtualParticipantRole.Participant,
            t0.AddSeconds(10)
        );
        _ = session.RecordParticipantLeft(
            $"person:{retiredId:D}",
            retiredId,
            room.MinimumAttendanceDwellSeconds,
            t0.AddSeconds(60),
            out _
        );
        Assert.Equal(2, session.Participants.Count);
        Assert.True(
            session.RepointAndMergeParticipants(
                retiredId,
                survivingId,
                room.MinimumAttendanceDwellSeconds
            )
        );
        var mergedParticipant = Assert.Single(session.Participants);
        Assert.Equal(390, mergedParticipant.CumulativeDwellSeconds);

        var rec = session.RegisterRecordingStart(
            "EG_001",
            "ruth",
            "virtual-rooms/recordings/session-1.mp4",
            t0
        );
        Assert.Equal(VirtualRecordingStatus.Active, rec.Status);

        var done = session.CompleteOrFailRecording(
            egressId: "EG_001",
            defaultBucket: "ruth",
            objectKey: "virtual-rooms/recordings/session-1.mp4",
            sizeBytes: 1_048_576,
            durationSeconds: 600,
            errorMessage: null,
            eventAtUtc: t0.AddMinutes(10)
        );
        Assert.Equal(VirtualRecordingStatus.Completed, done.Status);
        Assert.Equal(
            "virtual-rooms/recordings/session-1.transcript.json",
            done.TranscriptObjectKey
        );

        var failed = session.CompleteOrFailRecording(
            egressId: "EG_ERR",
            defaultBucket: "ruth",
            objectKey: null,
            sizeBytes: 0,
            durationSeconds: 0,
            errorMessage: "S3 bucket unreachable",
            eventAtUtc: t0.AddMinutes(12)
        );
        Assert.Equal(VirtualRecordingStatus.Failed, failed.Status);
        _ = session.CloseSession(room.MinimumAttendanceDwellSeconds, t0.AddHours(1));
        Assert.True(session.IsClosed);
    }

    [Fact]
    public void LiveKitJwtAuthenticator_MintsRoleScopedGrants_AndVerifiesWebhookSignaturesConstantTime()
    {
        const string apiKey = "lk-key";
        const string apiSecret = "lk-super-secret-hmac-key-2026-cleanroom!";
        var now = new DateTimeOffset(2026, 11, 25, 21, 0, 0, TimeSpan.Zero);

        var modJwt = LiveKitJwtAuthenticator.MintParticipantToken(
            apiKey,
            apiSecret,
            new VirtualRoomTokenSpec(
                "ruth-cohort",
                "person:1",
                "Moderator",
                VirtualParticipantRole.HostModerator,
                TimeSpan.FromMinutes(15)
            ),
            now
        );
        using var modDoc = DecodeJwtPayload(modJwt);
        Assert.True(modDoc.RootElement.GetProperty("video").GetProperty("roomAdmin").GetBoolean());
        Assert.True(modDoc.RootElement.GetProperty("video").GetProperty("canPublish").GetBoolean());

        var obsJwt = LiveKitJwtAuthenticator.MintParticipantToken(
            apiKey,
            apiSecret,
            new VirtualRoomTokenSpec(
                "ruth-cohort",
                "person:2",
                "Observer",
                VirtualParticipantRole.Observer,
                TimeSpan.FromMinutes(15)
            ),
            now
        );
        using var obsDoc = DecodeJwtPayload(obsJwt);
        Assert.False(obsDoc.RootElement.GetProperty("video").GetProperty("roomAdmin").GetBoolean());
        Assert.False(
            obsDoc.RootElement.GetProperty("video").GetProperty("canPublish").GetBoolean()
        );

        var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(new { @event = "participant_joined" });
        var webhookJwt = LiveKitJwtAuthenticator.SignWebhookPayload(
            apiKey,
            apiSecret,
            bodyBytes,
            now
        );

        Assert.True(
            LiveKitJwtAuthenticator.VerifyWebhook(
                $"Bearer {webhookJwt}",
                bodyBytes,
                apiKey,
                apiSecret,
                now
            )
        );
        Assert.False(
            LiveKitJwtAuthenticator.VerifyWebhook(
                webhookJwt,
                JsonSerializer.SerializeToUtf8Bytes(new { @event = "tampered" }),
                apiKey,
                apiSecret,
                now
            )
        );
        Assert.False(
            LiveKitJwtAuthenticator.VerifyWebhook(
                webhookJwt,
                bodyBytes,
                "wrong-key",
                apiSecret,
                now
            )
        );
        Assert.False(
            LiveKitJwtAuthenticator.VerifyWebhook(
                webhookJwt,
                bodyBytes,
                apiKey,
                "wrong-secret",
                now
            )
        );
        Assert.False(
            LiveKitJwtAuthenticator.VerifyWebhook(
                webhookJwt,
                bodyBytes,
                apiKey,
                apiSecret,
                now.AddHours(2)
            )
        );
        Assert.False(
            LiveKitJwtAuthenticator.VerifyWebhook("not-a-jwt", bodyBytes, apiKey, apiSecret, now)
        );

        var hexClaimPayload = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                iss = apiKey,
                nbf = now.ToUnixTimeSeconds() - 5,
                exp = now.ToUnixTimeSeconds() + 300,
                sha256 = Convert.ToHexStringLower(SHA256.HashData(bodyBytes)),
            }
        );
        var hdr = Base64Url.EncodeToString(
            JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" })
        );
        var pld = Base64Url.EncodeToString(hexClaimPayload);
        var sig = Base64Url.EncodeToString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(apiSecret),
                Encoding.ASCII.GetBytes($"{hdr}.{pld}")
            )
        );
        Assert.True(
            LiveKitJwtAuthenticator.VerifyWebhook(
                $"{hdr}.{pld}.{sig}",
                bodyBytes,
                apiKey,
                apiSecret,
                now
            )
        );
    }

    [Fact]
    public async Task LiveKitRoomProvider_TwirpRoomAndRustFsS3EgressCalls_MapSuccessAndHttpErrors()
    {
        var now = new DateTimeOffset(2026, 11, 25, 21, 30, 0, TimeSpan.Zero);
        var options = Options.Create(
            new LiveKitOptions
            {
                ServerUrl = "http://livekit.local:7880",
                PublicWebSocketUrl = "wss://livekit.example.org",
                ApiKey = "twirp-key",
                ApiSecret = "twirp-secret-key-at-least-32-chars!!",
                S3Endpoint = "http://rustfs:9000",
                S3Bucket = "ruth",
            }
        );

        var okFactory = new StubTwirpHttpClientFactory(
            (req, _) =>
                req.RequestUri!.AbsolutePath.Contains("Egress", StringComparison.Ordinal)
                    ? (
                        HttpStatusCode.OK,
                        JsonSerializer.Serialize(new { egress_id = "EG_TWIRP_99" })
                    )
                    : (HttpStatusCode.OK, JsonSerializer.Serialize(new { sid = "RM_TWIRP_01" }))
        );
        var provider = new LiveKitRoomProvider(okFactory, options, new FixedClock(now));

        var created = await provider.EnsureRoomAsync(
            new VirtualRoomProvisionSpec(
                "ruth-dev-room",
                MaxParticipants: 50,
                EmptyTimeoutSeconds: 300
            )
        );
        Assert.True(created.IsSuccess);
        Assert.Equal("RM_TWIRP_01", created.Value.LiveKitRoomSid);

        var egress = await provider.StartRoomCompositeEgressAsync(
            new VirtualRoomEgressSpec("ruth-dev-room", "ruth", "virtual-rooms/rec/out.mp4")
        );
        Assert.True(egress.IsSuccess);
        Assert.Equal("EG_TWIRP_99", egress.Value.EgressId);
        Assert.Contains(okFactory.Requests, static r =>
            r.Body.Contains("\"force_path_style\":true", StringComparison.Ordinal)
        );

        Assert.True((await provider.StopEgressAsync("EG_TWIRP_99")).IsSuccess);
        Assert.True((await provider.CloseRoomAsync("ruth-dev-room")).IsSuccess);

        var failFactory = new StubTwirpHttpClientFactory(
            (_, _) =>
                (
                    HttpStatusCode.BadGateway,
                    JsonSerializer.Serialize(new { msg = "upstream error" })
                )
        );
        var failProvider = new LiveKitRoomProvider(failFactory, options, new FixedClock(now));
        Assert.True(
            (
                await failProvider.EnsureRoomAsync(
                    new VirtualRoomProvisionSpec(
                        "ruth-err",
                        MaxParticipants: 10,
                        EmptyTimeoutSeconds: 60
                    )
                )
            ).IsFailure
        );
        Assert.True((await failProvider.StopEgressAsync("EG_ERR")).IsFailure);
    }

    private static JsonDocument DecodeJwtPayload(string jwt)
    {
        var parts = jwt.Split('.');
        return JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
        public DateTimeOffset LocalNow => utcNow;
        public DateOnly TodayUtc => DateOnly.FromDateTime(utcNow.UtcDateTime);
        public DateOnly TodayLocal => TodayUtc;
        public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public TimeProvider TimeProvider => TimeProvider.System;

        public DateTimeOffset ConvertToLocal(DateTimeOffset utcTimestamp)
        {
            return utcTimestamp.ToUniversalTime();
        }

        public long GetTimestamp()
        {
            return TimeProvider.System.GetTimestamp();
        }

        public TimeSpan GetElapsedTime(long startingTimestamp)
        {
            return TimeProvider.System.GetElapsedTime(startingTimestamp);
        }
    }
}
