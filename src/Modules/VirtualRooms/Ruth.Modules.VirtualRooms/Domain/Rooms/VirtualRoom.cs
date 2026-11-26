using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.VirtualRooms.Domain.Rooms;

/// <summary>Organization-agnostic embedded LiveKit WebRTC VirtualRoom aggregate.</summary>
public sealed class VirtualRoom
    : AggregateRoot<VirtualRoomId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.VirtualRooms.VirtualRoom";

    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private VirtualRoom() { }

    private VirtualRoom(VirtualRoomId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string LiveKitRoomName { get; private set; } = string.Empty;
    public VirtualRoomContextKind ContextKind { get; private set; }
    public string? Description { get; private set; }
    public Guid? LinkedGroupId { get; private set; }
    public Guid? LinkedLocationId { get; private set; }
    public Guid? LinkedScheduleId { get; private set; }
    public Guid? LinkedBranchId { get; private set; }
    public Guid? LinkedBookingId { get; private set; }
    public Guid? LinkedSupportRequestId { get; private set; }
    public Guid? LinkedEventItemOccurrenceId { get; private set; }
    public Guid? LinkedLmsClassId { get; private set; }
    public string? ExternalReferenceKey { get; private set; }
    public Guid? HostModeratorPersonId { get; private set; }
    public int MaxParticipants { get; private set; } = 100;
    public int EmptyTimeoutSeconds { get; private set; } = 300;
    public bool AutoRecordAttendance { get; private set; } = true;
    public int MinimumAttendanceDwellSeconds { get; private set; } = 180;
    public bool AutoStartRecordingEgress { get; private set; }
    public bool IsWebinarBroadcast { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<VirtualRoom> Create(VirtualRoomSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new VirtualRoom(VirtualRoomId.Create());
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<VirtualRoom>(res.PrimaryError)
            : Result.Success(entity);
    }

    public Result Update(VirtualRoomSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return Apply(spec);
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (HostModeratorPersonId != retiredPersonId)
        {
            return false;
        }

        HostModeratorPersonId = survivingPersonId;
        return true;
    }

    private Result Apply(VirtualRoomSpec spec)
    {
        if (!TryValidateBounds(spec, out var slug))
        {
            return Result.Failure(VirtualRoomsErrors.RoomInvalid);
        }

        AssignRoomMetadata(spec, slug);
        return Result.Success();
    }

    private static bool TryValidateBounds(VirtualRoomSpec spec, out string slug)
    {
        slug = string.Empty;
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.MaxParticipants is >= 2 and <= 2000
            && spec.EmptyTimeoutSeconds >= 30
            && spec.MinimumAttendanceDwellSeconds >= 10
            && VirtualRoomsTextNormalizer.TryNormalizeSlug(spec.Slug, out slug);
    }

    private void AssignRoomMetadata(VirtualRoomSpec spec, string slug)
    {
        Name = spec.Name.Trim();
        Slug = slug;
        if (string.IsNullOrWhiteSpace(LiveKitRoomName))
        {
            LiveKitRoomName = $"ruth-{slug}-{Id.Value.ToString("N")[..8]}";
        }

        ContextKind = spec.ContextKind;
        Description = VirtualRoomsTextNormalizer.TrimOrNull(spec.Description);
        LinkedGroupId = spec.LinkedGroupId;
        LinkedLocationId = spec.LinkedLocationId;
        LinkedScheduleId = spec.LinkedScheduleId;
        LinkedBranchId = spec.LinkedBranchId;
        LinkedBookingId = spec.LinkedBookingId;
        LinkedSupportRequestId = spec.LinkedSupportRequestId;
        LinkedEventItemOccurrenceId = spec.LinkedEventItemOccurrenceId;
        LinkedLmsClassId = spec.LinkedLmsClassId;
        ExternalReferenceKey = VirtualRoomsTextNormalizer.TrimOrNull(
            spec.ExternalReferenceKey,
            160
        );
        HostModeratorPersonId = spec.HostModeratorPersonId;
        MaxParticipants = spec.MaxParticipants;
        EmptyTimeoutSeconds = spec.EmptyTimeoutSeconds;
        AutoRecordAttendance = spec.AutoRecordAttendance;
        MinimumAttendanceDwellSeconds = spec.MinimumAttendanceDwellSeconds;
        AutoStartRecordingEgress = spec.AutoStartRecordingEgress;
        IsWebinarBroadcast = spec.IsWebinarBroadcast;
        IsActive = spec.IsActive;
    }
}
