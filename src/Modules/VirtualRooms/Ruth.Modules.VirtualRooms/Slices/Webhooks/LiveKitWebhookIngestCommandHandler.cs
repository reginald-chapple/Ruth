using System.Text;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Contracts.Webhooks;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.Modules.VirtualRooms.Slices.Rooms;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Webhooks;

internal sealed class LiveKitWebhookIngestCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    VirtualRoomAttendanceCollaborator attendanceCollaborator,
    LiveKitWebhookEgressCollaborator egressCollaborator,
    IClock clock
) : ICommandHandler<LiveKitWebhookIngestCommand, LiveKitWebhookIngestResponse>
{
    public async Task<Result<LiveKitWebhookIngestResponse>> HandleAsync(
        LiveKitWebhookIngestCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var validated = VerifyAndParse(command);
        if (validated.IsFailure)
        {
            return Result.Failure<LiveKitWebhookIngestResponse>(validated.PrimaryError);
        }

        var parsed = validated.Value;
        var room = string.IsNullOrWhiteSpace(parsed.RoomName)
            ? null
            : await dbContext.VirtualRooms.FirstOrDefaultAsync(
                r => r.LiveKitRoomName == parsed.RoomName,
                cancellationToken
            );
        if (room is null)
        {
            return Result.Success(
                new LiveKitWebhookIngestResponse(
                    parsed.EventType,
                    MatchedSessionId: null,
                    AttendanceRecorded: false,
                    RecordingArchived: false
                )
            );
        }

        var session = await ResolveOrCreateSessionAsync(room, parsed, cancellationToken);
        var (attendanceRecorded, recordingArchived) = await ApplyEventAsync(
            room,
            session,
            parsed,
            cancellationToken
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            new LiveKitWebhookIngestResponse(
                parsed.EventType,
                session.Id.Value,
                attendanceRecorded,
                recordingArchived
            )
        );
    }

    private Result<ParsedLiveKitWebhook> VerifyAndParse(LiveKitWebhookIngestCommand command)
    {
        var rawBytes = Encoding.UTF8.GetBytes(command.RawPayloadJson ?? string.Empty);
        var provider = providerSelector.ResolveActive();
        if (!provider.VerifyWebhookSignature(command.AuthorizationHeader, rawBytes, clock.UtcNow))
        {
            return Result.Failure<ParsedLiveKitWebhook>(VirtualRoomsErrors.WebhookSignatureInvalid);
        }

        var parsed = LiveKitWebhookPayloadParser.TryParse(
            command.RawPayloadJson ?? string.Empty,
            clock.UtcNow
        );
        return parsed is null
            ? Result.Failure<ParsedLiveKitWebhook>(
                DomainError.Validation(
                    "VirtualRooms.WebhookPayloadInvalid",
                    "LiveKit webhook JSON payload could not be parsed."
                )
            )
            : Result.Success(parsed);
    }

    private async Task<VirtualRoomSession> ResolveOrCreateSessionAsync(
        VirtualRoom room,
        ParsedLiveKitWebhook parsed,
        CancellationToken ct
    )
    {
        var existing = await dbContext
            .VirtualRoomSessions.AsSplitQuery()
            .Include(static s => s.Participants)
            .Include(static s => s.Recordings)
            .Where(s =>
                s.VirtualRoomId == room.Id
                && (
                    s.Status == VirtualRoomSessionStatus.Live
                    || s.Status == VirtualRoomSessionStatus.Scheduled
                )
            )
            .OrderByDescending(static s => s.ScheduledStartUtc)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return existing;
        }

        var fallback = VirtualRoomSession
            .Create(
                room,
                $"{room.Name} Session",
                parsed.OccurredAtUtc,
                parsed.OccurredAtUtc.AddHours(2)
            )
            .Value;
        _ = fallback.MarkLive(parsed.RoomSid, parsed.OccurredAtUtc);
        _ = dbContext.VirtualRoomSessions.Add(fallback);
        return fallback;
    }

    private async Task<(bool AttendanceRecorded, bool RecordingArchived)> ApplyEventAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed,
        CancellationToken ct
    )
    {
        var eventKey = parsed.EventType.ToLowerInvariant();
        return eventKey switch
        {
            "room_started" => HandleRoomStarted(session, parsed),
            "participant_joined" => HandleParticipantJoined(room, session, parsed),
            "participant_left" => await HandleParticipantLeftAsync(room, session, parsed, ct),
            "room_finished" => await HandleRoomFinishedAsync(room, session, parsed, ct),
            "egress_started" => egressCollaborator.HandleEgressStarted(session, parsed),
            "egress_ended" => await egressCollaborator.HandleEgressEndedAsync(
                room,
                session,
                parsed,
                ct
            ),
            _ => (false, false),
        };
    }

    private static (bool, bool) HandleRoomStarted(
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed
    )
    {
        _ = session.MarkLive(parsed.RoomSid, parsed.OccurredAtUtc);
        return (false, false);
    }

    private static (bool, bool) HandleParticipantJoined(
        VirtualRoom room,
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed
    )
    {
        if (!string.IsNullOrWhiteSpace(parsed.ParticipantIdentity))
        {
            var role =
                parsed.ParticipantPersonId.HasValue
                && room.HostModeratorPersonId == parsed.ParticipantPersonId
                    ? VirtualParticipantRole.HostModerator
                    : VirtualParticipantRole.Participant;
            _ = session.RecordParticipantJoined(
                parsed.ParticipantIdentity,
                parsed.ParticipantPersonId,
                parsed.ParticipantName ?? "Participant",
                role,
                parsed.OccurredAtUtc
            );
        }

        return (false, false);
    }

    private async Task<(bool, bool)> HandleParticipantLeftAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(parsed.ParticipantIdentity))
        {
            return (false, false);
        }

        var participant = session.RecordParticipantLeft(
            parsed.ParticipantIdentity,
            parsed.ParticipantPersonId,
            room.MinimumAttendanceDwellSeconds,
            parsed.OccurredAtUtc,
            out _
        );
        if (participant is null)
        {
            return (false, false);
        }

        var recorded = await attendanceCollaborator.TryRecordQualifiedAttendanceAsync(
            room,
            session,
            participant,
            ct
        );
        return (recorded, false);
    }

    private async Task<(bool, bool)> HandleRoomFinishedAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        ParsedLiveKitWebhook parsed,
        CancellationToken ct
    )
    {
        var qualifiedList = session.CloseSession(
            room.MinimumAttendanceDwellSeconds,
            parsed.OccurredAtUtc
        );
        var anyRecorded = false;
        foreach (var p in qualifiedList)
        {
            anyRecorded |= await attendanceCollaborator.TryRecordQualifiedAttendanceAsync(
                room,
                session,
                p,
                ct
            );
        }

        return (anyRecorded, false);
    }
}
