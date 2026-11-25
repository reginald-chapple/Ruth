using Ruth.Modules.Crm.Contracts.Relationships;

namespace Ruth.Modules.Crm.Domain.Relationships;

/// <summary>Resolves reciprocal inverse relationship types for bidirectional known relationships.</summary>
public static class KnownRelationshipInverseMap
{
    /// <summary>Returns the reciprocal relationship type owned by the target person.</summary>
    public static KnownRelationshipType GetInverse(KnownRelationshipType relationshipType)
    {
        return relationshipType switch
        {
            KnownRelationshipType.CanCheckIn => KnownRelationshipType.AllowCheckInBy,
            KnownRelationshipType.AllowCheckInBy => KnownRelationshipType.CanCheckIn,
            KnownRelationshipType.Parent => KnownRelationshipType.Child,
            KnownRelationshipType.Child => KnownRelationshipType.Parent,
            KnownRelationshipType.Grandparent => KnownRelationshipType.Grandchild,
            KnownRelationshipType.Grandchild => KnownRelationshipType.Grandparent,
            KnownRelationshipType.Mentor => KnownRelationshipType.Mentee,
            KnownRelationshipType.Mentee => KnownRelationshipType.Mentor,
            KnownRelationshipType.Employer => KnownRelationshipType.Employee,
            KnownRelationshipType.Employee => KnownRelationshipType.Employer,
            KnownRelationshipType.EmergencyContact => KnownRelationshipType.EmergencyContactFor,
            KnownRelationshipType.EmergencyContactFor => KnownRelationshipType.EmergencyContact,
            KnownRelationshipType.Sibling => KnownRelationshipType.Sibling,
            KnownRelationshipType.Spouse => KnownRelationshipType.Spouse,
            KnownRelationshipType.Friend => KnownRelationshipType.Friend,
            _ => relationshipType,
        };
    }
}
