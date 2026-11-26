namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Lifecycle states for a bulk or targeted Communication campaign.</summary>
public enum CommunicationStatus
{
    /// <summary>Editable draft not yet submitted for review or dispatch.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting reviewer approval before dispatch.</summary>
    PendingApproval = 1,

    /// <summary>Approved and queued for immediate or scheduled batch dispatch.</summary>
    Approved = 2,

    /// <summary>Actively dispatching recipient batches through configured transports.</summary>
    Sending = 3,

    /// <summary>All recipient delivery attempts have completed.</summary>
    Sent = 4,

    /// <summary>Returned by an approver for revision.</summary>
    Rejected = 5,

    /// <summary>Cancelled prior to completion.</summary>
    Cancelled = 6,
}
