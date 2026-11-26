namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Classification of an email or SMS delivery bounce or complaint.</summary>
public enum BounceKind
{
    /// <summary>No bounce recorded.</summary>
    None = 0,

    /// <summary>Transient soft bounce (mailbox full, temporary throttling).</summary>
    Soft = 1,

    /// <summary>Permanent hard bounce (invalid address, domain does not exist).</summary>
    Hard = 2,

    /// <summary>Recipient marked message as spam/abuse, triggering suppression.</summary>
    SpamComplaint = 3,
}
