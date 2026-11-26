namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Editorial approval status for a ContentChannelItem.</summary>
public enum ContentChannelItemStatus
{
    /// <summary>Awaiting editorial review before public publication.</summary>
    PendingApproval = 1,

    /// <summary>Approved for scheduled or immediate publication.</summary>
    Approved = 2,

    /// <summary>Rejected during editorial review.</summary>
    Denied = 3,
}
