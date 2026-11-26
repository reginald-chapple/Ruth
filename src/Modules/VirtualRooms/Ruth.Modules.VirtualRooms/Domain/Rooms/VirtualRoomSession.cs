using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Domain.Rooms;

/// <summary>Live or scheduled VirtualRoomSession occurrence tracking participant dwell and RustFS recording archives.</summary>
public sealed partial class VirtualRoomSession
{
    private readonly List<VirtualRoomParticipantSession> _participants = [];
    private readonly List<VirtualRoomRecordingArchive> _recordings = [];

    private VirtualRoomSession() { }

    private VirtualRoomSession(VirtualRoomSessionId id)
        : base(id) { }

    public VirtualRoomId VirtualRoomId { get; private set; }
    public string? LiveKitRoomSid { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public VirtualRoomSessionStatus Status { get; private set; } =
        VirtualRoomSessionStatus.Scheduled;
    public DateTimeOffset ScheduledStartUtc { get; private set; }
    public DateTimeOffset ScheduledEndUtc { get; private set; }
    public DateTimeOffset? ActualStartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public bool IsClosed =>
        Status is VirtualRoomSessionStatus.Ended or VirtualRoomSessionStatus.Cancelled;

    public IReadOnlyList<VirtualRoomParticipantSession> Participants => _participants;
    public IReadOnlyList<VirtualRoomRecordingArchive> Recordings => _recordings;

    public static Result<VirtualRoomSession> Create(
        VirtualRoom room,
        string title,
        DateTimeOffset scheduledStartUtc,
        DateTimeOffset scheduledEndUtc
    )
    {
        ArgumentNullException.ThrowIfNull(room);
        if (!room.IsActive)
        {
            return Result.Failure<VirtualRoomSession>(VirtualRoomsErrors.RoomInactive);
        }

        var start = scheduledStartUtc.ToUniversalTime();
        var end = scheduledEndUtc.ToUniversalTime();
        return string.IsNullOrWhiteSpace(title) || end <= start
            ? Result.Failure<VirtualRoomSession>(VirtualRoomsErrors.SessionInvalid)
            : Result.Success(
                new VirtualRoomSession(VirtualRoomSessionId.Create())
                {
                    VirtualRoomId = room.Id,
                    Title = title.Trim(),
                    Status = VirtualRoomSessionStatus.Scheduled,
                    ScheduledStartUtc = start,
                    ScheduledEndUtc = end,
                }
            );
    }

    public Result MarkLive(string? liveKitRoomSid, DateTimeOffset nowUtc)
    {
        if (IsClosed)
        {
            return Result.Failure(VirtualRoomsErrors.SessionAlreadyClosed);
        }

        Status = VirtualRoomSessionStatus.Live;
        ActualStartedAtUtc ??= nowUtc.ToUniversalTime();
        if (!string.IsNullOrWhiteSpace(liveKitRoomSid))
        {
            LiveKitRoomSid = liveKitRoomSid.Trim();
        }

        return Result.Success();
    }

    public VirtualRoomParticipantSession RecordParticipantJoined(
        string participantIdentity,
        Guid? personId,
        string displayName,
        VirtualParticipantRole role,
        DateTimeOffset joinedAtUtc
    )
    {
        if (Status == VirtualRoomSessionStatus.Scheduled)
        {
            Status = VirtualRoomSessionStatus.Live;
            ActualStartedAtUtc ??= joinedAtUtc.ToUniversalTime();
        }

        var existing = FindParticipant(participantIdentity, personId);
        if (existing is not null)
        {
            existing.RecordRejoin(personId, displayName, role, joinedAtUtc);
            return existing;
        }

        var created = VirtualRoomParticipantSession.StartJoin(
            Id,
            participantIdentity,
            personId,
            displayName,
            role,
            joinedAtUtc
        );
        _participants.Add(created);
        return created;
    }

    public VirtualRoomParticipantSession? RecordParticipantLeft(
        string participantIdentity,
        Guid? personId,
        int minimumDwellSeconds,
        DateTimeOffset leftAtUtc,
        out bool newlyQualified
    )
    {
        newlyQualified = false;
        var participant = FindParticipant(participantIdentity, personId);
        if (participant is null)
        {
            return null;
        }

        newlyQualified = participant.RecordLeaveAndCheckThreshold(leftAtUtc, minimumDwellSeconds);
        return participant;
    }

    public IReadOnlyList<VirtualRoomParticipantSession> CloseSession(
        int minimumDwellSeconds,
        DateTimeOffset endedAtUtc
    )
    {
        var normalized = endedAtUtc.ToUniversalTime();
        var newlyQualified = new List<VirtualRoomParticipantSession>();
        foreach (var participant in _participants)
        {
            if (participant.RecordLeaveAndCheckThreshold(normalized, minimumDwellSeconds))
            {
                newlyQualified.Add(participant);
            }
        }

        Status = VirtualRoomSessionStatus.Ended;
        EndedAtUtc ??= normalized;
        return newlyQualified;
    }

    public VirtualRoomRecordingArchive RegisterRecordingStart(
        string egressId,
        string bucket,
        string objectKey,
        DateTimeOffset startedAtUtc
    )
    {
        var existing = _recordings.Find(r =>
            string.Equals(r.EgressId, egressId.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        if (existing is not null)
        {
            return existing;
        }

        var archive = VirtualRoomRecordingArchive.CreateStarting(
            Id,
            egressId,
            bucket,
            objectKey,
            startedAtUtc
        );
        _recordings.Add(archive);
        return archive;
    }

    public VirtualRoomRecordingArchive CompleteOrFailRecording(
        string egressId,
        string defaultBucket,
        string? objectKey,
        long sizeBytes,
        int durationSeconds,
        string? errorMessage,
        DateTimeOffset eventAtUtc
    )
    {
        var existing = _recordings.Find(r =>
            string.Equals(r.EgressId, egressId.Trim(), StringComparison.OrdinalIgnoreCase)
        )
            ?? RegisterRecordingStart(
                egressId,
                defaultBucket,
                objectKey ?? $"virtual-rooms/recordings/{Id.Value:D}/{egressId.Trim()}.mp4",
                eventAtUtc
            );

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            existing.MarkFailed(errorMessage, eventAtUtc);
        }
        else
        {
            existing.MarkCompleted(sizeBytes, durationSeconds, objectKey, eventAtUtc);
        }

        return existing;
    }

    private VirtualRoomParticipantSession? FindParticipant(
        string participantIdentity,
        Guid? personId
    )
    {
        return _participants.Find(p =>
            string.Equals(
                p.ParticipantIdentity,
                participantIdentity.Trim(),
                StringComparison.OrdinalIgnoreCase
            ) || (personId.HasValue && p.PersonId == personId.Value)
        );
    }
}
