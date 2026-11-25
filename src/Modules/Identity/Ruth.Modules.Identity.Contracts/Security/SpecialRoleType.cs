namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Built-in special principal groups evaluated by entity ACL rules.</summary>
public enum SpecialRoleType
{
    /// <summary>Rule targets a specific security role or user account rather than a special role.</summary>
    None = 0,

    /// <summary>Rule matches every caller (authenticated and anonymous).</summary>
    AllUsers = 1,

    /// <summary>Rule matches any authenticated caller.</summary>
    AllAuthenticatedUsers = 2,

    /// <summary>Rule matches only unauthenticated (anonymous) callers.</summary>
    AllUnauthenticatedUsers = 3,
}
