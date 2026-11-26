using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.VirtualRooms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a VirtualRoomRecordingArchive.</summary>
public readonly record struct VirtualRoomRecordingArchiveId(Guid Value)
    : IStronglyTypedId<VirtualRoomRecordingArchiveId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static VirtualRoomRecordingArchiveId Empty => new(Guid.Empty);

    public static VirtualRoomRecordingArchiveId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static VirtualRoomRecordingArchiveId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
