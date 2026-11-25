namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Approval workflow status of a note.</summary>
public enum NoteApprovalState
{
    /// <summary>Awaiting moderator approval.</summary>
    PendingApproval = 0,

    /// <summary>Approved and visible to authorized viewers.</summary>
    Approved = 1,

    /// <summary>Denied by moderator.</summary>
    Denied = 2,
}
