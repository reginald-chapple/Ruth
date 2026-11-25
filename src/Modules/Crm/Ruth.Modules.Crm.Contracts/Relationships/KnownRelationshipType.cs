namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Known interpersonal relationship types with reciprocal inverse pairs.</summary>
public enum KnownRelationshipType
{
    /// <summary>Owner can check in the related person.</summary>
    CanCheckIn = 0,

    /// <summary>Owner allows check-in by the related person (inverse of CanCheckIn).</summary>
    AllowCheckInBy = 1,

    /// <summary>Owner is a parent of the related person.</summary>
    Parent = 2,

    /// <summary>Owner is a child of the related person (inverse of Parent).</summary>
    Child = 3,

    /// <summary>Symmetric sibling relationship.</summary>
    Sibling = 4,

    /// <summary>Owner is a grandparent of the related person.</summary>
    Grandparent = 5,

    /// <summary>Owner is a grandchild of the related person (inverse of Grandparent).</summary>
    Grandchild = 6,

    /// <summary>Symmetric spouse relationship.</summary>
    Spouse = 7,

    /// <summary>Owner mentors the related person.</summary>
    Mentor = 8,

    /// <summary>Owner is mentored by the related person (inverse of Mentor).</summary>
    Mentee = 9,

    /// <summary>Owner employs the related person.</summary>
    Employer = 10,

    /// <summary>Owner is employed by the related person (inverse of Employer).</summary>
    Employee = 11,

    /// <summary>Owner is an emergency contact for the related person.</summary>
    EmergencyContact = 12,

    /// <summary>Owner has the related person as an emergency contact.</summary>
    EmergencyContactFor = 13,

    /// <summary>Symmetric friend relationship.</summary>
    Friend = 14,
}
