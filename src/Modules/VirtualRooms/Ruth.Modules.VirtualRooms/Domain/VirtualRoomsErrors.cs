using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Domain errors for the VirtualRooms bounded context (W5-S7).</summary>
public static class VirtualRoomsErrors
{
    public static readonly DomainError RoomInvalid = DomainError.Validation(
        "VirtualRooms.RoomInvalid",
        "VirtualRoom requires a valid name, slug, capacity (2..2000), timeout (>= 30s), and minimum dwell threshold (>= 10s)."
    );

    public static readonly DomainError RoomNotFound = DomainError.NotFound(
        "VirtualRooms.RoomNotFound",
        "VirtualRoom was not found."
    );

    public static readonly DomainError RoomSlugConflict = DomainError.Conflict(
        "VirtualRooms.RoomSlugConflict",
        "A VirtualRoom with the specified slug already exists."
    );

    public static readonly DomainError RoomInactive = DomainError.Conflict(
        "VirtualRooms.RoomInactive",
        "Cannot start sessions or mint join tokens for an inactive VirtualRoom."
    );

    public static readonly DomainError SessionInvalid = DomainError.Validation(
        "VirtualRooms.SessionInvalid",
        "VirtualRoomSession requires a non-empty title and ScheduledEndUtc strictly after ScheduledStartUtc."
    );

    public static readonly DomainError SessionNotFound = DomainError.NotFound(
        "VirtualRooms.SessionNotFound",
        "VirtualRoomSession was not found."
    );

    public static readonly DomainError SessionAlreadyClosed = DomainError.Conflict(
        "VirtualRooms.SessionAlreadyClosed",
        "Ended or cancelled VirtualRoomSessions cannot be mutated."
    );

    public static readonly DomainError JoinAccessForbidden = DomainError.Forbidden(
        "VirtualRooms.JoinAccessForbidden",
        "Caller is not authorized by Ruth ACL rules to join this VirtualRoom."
    );

    public static readonly DomainError JoinDisplayNameRequired = DomainError.Validation(
        "VirtualRooms.JoinDisplayNameRequired",
        "Participant display name is required when minting a room join token."
    );

    public static readonly DomainError RecordingNotFound = DomainError.NotFound(
        "VirtualRooms.RecordingNotFound",
        "Recording egress archive was not found."
    );

    public static readonly DomainError WebhookSignatureInvalid = DomainError.Unauthorized(
        "VirtualRooms.WebhookSignatureInvalid",
        "LiveKit webhook Authorization JWT or SHA-256 payload digest failed verification."
    );
}
