namespace Ruth.SharedKernel.Security;

/// <summary>Canonical authorization action names shared across bounded contexts.</summary>
public static class AuthorizationActions
{
    /// <summary>Permission to view an entity.</summary>
    public const string View = "View";

    /// <summary>Permission to view all entities overriding local filters.</summary>
    public const string ViewAll = "ViewAll";

    /// <summary>Permission to edit or update an entity.</summary>
    public const string Edit = "Edit";

    /// <summary>Permission to delete an entity where distinct from Edit.</summary>
    public const string Delete = "Delete";

    /// <summary>Permission to administer an entity and modify its security rules.</summary>
    public const string Administrate = "Administrate";

    /// <summary>Permission to approve content or workflow items.</summary>
    public const string Approve = "Approve";

    /// <summary>Permission to interact with content or resources.</summary>
    public const string Interact = "Interact";

    /// <summary>Permission to tag entities.</summary>
    public const string Tag = "Tag";

    /// <summary>Permission to execute an operation or endpoint.</summary>
    public const string Execute = "Execute";

    /// <summary>Permission to execute read API operations.</summary>
    public const string ExecuteRead = "ExecuteRead";

    /// <summary>Permission to execute write API operations.</summary>
    public const string ExecuteWrite = "ExecuteWrite";

    /// <summary>Permission to view entity listings.</summary>
    public const string ViewList = "ViewList";

    /// <summary>Standard View/Edit/Administrate action descriptions.</summary>
    public static IReadOnlyDictionary<string, string> StandardActions { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [View] = "The roles and/or users that have access to view.",
            [Edit] = "The roles and/or users that have access to edit.",
            [Administrate] = "The roles and/or users that have access to administrate.",
        };
}
