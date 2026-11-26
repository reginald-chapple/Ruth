namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Lifecycle status of an assistance or grant case.</summary>
public enum AssistanceCaseStatus
{
    /// <summary>Initial intake submitted and awaiting review.</summary>
    Submitted = 0,

    /// <summary>Assigned to a caseworker or committee for verification.</summary>
    UnderReview = 1,

    /// <summary>Approved with an authorized award ceiling awaiting disbursement.</summary>
    Approved = 2,

    /// <summary>Partially disbursed against the approved award ceiling.</summary>
    PartiallyDisbursed = 3,

    /// <summary>Approved award ceiling has been fully disbursed.</summary>
    Disbursed = 4,

    /// <summary>Case request was declined.</summary>
    Denied = 5,

    /// <summary>Case was administratively closed.</summary>
    Closed = 6,
}
