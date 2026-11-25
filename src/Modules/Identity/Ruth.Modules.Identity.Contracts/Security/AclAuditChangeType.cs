namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Type of change recorded in the authorization audit log.</summary>
public enum AclAuditChangeType
{
    /// <summary>A new ACL rule was added.</summary>
    Added = 1,

    /// <summary>An existing ACL rule decision or priority changed.</summary>
    Modified = 2,

    /// <summary>An existing ACL rule was deleted.</summary>
    Deleted = 3,

    /// <summary>An existing ACL rule was reordered.</summary>
    Reordered = 4,
}
