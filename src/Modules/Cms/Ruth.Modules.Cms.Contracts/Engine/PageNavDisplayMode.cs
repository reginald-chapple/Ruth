namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Controls when a CMS page appears in site navigation menus.</summary>
public enum PageNavDisplayMode
{
    /// <summary>Display in navigation only when the current viewer has View access.</summary>
    WhenAllowed = 0,

    /// <summary>Always display in navigation regardless of viewer ACL.</summary>
    Always = 1,

    /// <summary>Never display in automated navigation trees.</summary>
    Never = 2,
}
