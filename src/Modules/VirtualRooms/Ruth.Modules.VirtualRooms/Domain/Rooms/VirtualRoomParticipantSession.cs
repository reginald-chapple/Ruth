using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain.Rooms;

/// <summary>Tracks monotonic dwell accumulation and auto-attendance qualification for a participant in a VirtualRoomSession.</summary>
public sealed class VirtualRoomParticipantSession : Entity<VirtualRoomParticipantAttendanceId>
{
    private VirtualRoomParticipantSession() { }

    private VirtualRoomParticipantSession(VirtualRoomParticipantAttendanceId id)
        : base(id) { }

    public VirtualRoomSessionId SessionId { get; private set; }
    public string ParticipantIdentity { get; private set; } = string.Empty;
    public Guid? PersonId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public VirtualParticipantRole Role { get; private set; }
    public DateTimeOffset FirstJoinedAtUtc { get; private set; }
    public DateTimeOffset LastJoinedAtUtc { get; private set; }
    public DateTimeOffset? LastLeftAtUtc { get; private set; }
    public bool IsCurrentlyConnected { get; private set; }
    public int JoinCount { get; private set; }
    public int CumulativeDwellSeconds { get; private set; }
    public bool QualifiedForAttendance { get; private set; }
    public DateTimeOffset? AttendanceRecordedAtUtc { get; private set; }

    internal static VirtualRoomParticipantSession StartJoin(
        VirtualRoomSessionId sessionId,
        string participantIdentity,
        Guid? personId,
        string displayName,
        VirtualParticipantRole role,
        DateTimeOffset joinedAtUtc
    )
    {
        var normalized = joinedAtUtc.ToUniversalTime();
        return new VirtualRoomParticipantSession(VirtualRoomParticipantAttendanceId.Create())
        {
            SessionId = sessionId,
            ParticipantIdentity = participantIdentity.Trim(),
            PersonId = personId,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? "Participant"
                : displayName.Trim(),
            Role = role,
            FirstJoinedAtUtc = normalized,
            LastJoinedAtUtc = normalized,
            IsCurrentlyConnected = true,
            JoinCount = 1,
        };
    }

    internal void RecordRejoin(
        Guid? personId,
        string? displayName,
        VirtualParticipantRole role,
        DateTimeOffset joinedAtUtc
    )
    {
        var normalized = joinedAtUtc.ToUniversalTime();
        if (personId.HasValue && !PersonId.HasValue)
        {
            PersonId = personId;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName.Trim();
        }

        Role = role;
        if (!IsCurrentlyConnected)
        {
            IsCurrentlyConnected = true;
            LastJoinedAtUtc = normalized;
            JoinCount++;
        }
    }

    internal bool RecordLeaveAndCheckThreshold(DateTimeOffset leftAtUtc, int minimumDwellSeconds)
    {
        var normalized = leftAtUtc.ToUniversalTime();
        if (IsCurrentlyConnected)
        {
            var elapsed = (int)Math.Max(0, (normalized - LastJoinedAtUtc).TotalSeconds);
            CumulativeDwellSeconds += elapsed;
            IsCurrentlyConnected = false;
            LastLeftAtUtc = normalized;
        }

        if (!QualifiedForAttendance && CumulativeDwellSeconds >= Math.Max(1, minimumDwellSeconds))
        {
            QualifiedForAttendance = true;
            return true;
        }

        return false;
    }

    internal void MarkAttendanceRecorded(DateTimeOffset recordedAtUtc)
    {
        AttendanceRecordedAtUtc ??= recordedAtUtc.ToUniversalTime();
    }

    internal void AbsorbDuplicateParticipant(
        VirtualRoomParticipantSession other,
        Guid survivingPersonId,
        int minimumDwellSeconds
    )
    {
        PersonId = survivingPersonId;
        CumulativeDwellSeconds += other.CumulativeDwellSeconds;
        JoinCount += other.JoinCount;
        if (other.FirstJoinedAtUtc < FirstJoinedAtUtc)
        {
            FirstJoinedAtUtc = other.FirstJoinedAtUtc;
        }

        if (CumulativeDwellSeconds >= Math.Max(1, minimumDwellSeconds))
        {
            QualifiedForAttendance = true;
        }

        AttendanceRecordedAtUtc ??= other.AttendanceRecordedAtUtc;
    }

    internal bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
