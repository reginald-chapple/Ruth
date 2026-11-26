using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.VirtualRooms.Domain.Rooms;

public sealed partial class VirtualRoomSession
    : AggregateRoot<VirtualRoomSessionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.VirtualRooms.VirtualRoomSession";

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

    public bool RepointAndMergeParticipants(
        Guid retiredPersonId,
        Guid survivingPersonId,
        int minimumDwellSeconds
    )
    {
        var retiredRow = _participants.Find(p => p.PersonId == retiredPersonId);
        if (retiredRow is null)
        {
            return false;
        }

        var survivingRow = _participants.Find(p => p.PersonId == survivingPersonId);
        if (survivingRow is not null && !ReferenceEquals(retiredRow, survivingRow))
        {
            survivingRow.AbsorbDuplicateParticipant(
                retiredRow,
                survivingPersonId,
                minimumDwellSeconds
            );
            _ = _participants.Remove(retiredRow);
            return true;
        }

        return retiredRow.RepointPerson(retiredPersonId, survivingPersonId);
    }
}
