namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Outreach communication channel for a Touchpoint.</summary>
public enum TouchpointChannel
{
    /// <summary>Voice phone call.</summary>
    PhoneCall = 0,

    /// <summary>SMS or direct message.</summary>
    SmsText = 1,

    /// <summary>Personal email.</summary>
    Email = 2,

    /// <summary>In-person coffee, visit, or meeting.</summary>
    InPersonMeeting = 3,

    /// <summary>Video / virtual room check-in.</summary>
    VirtualSession = 4,

    /// <summary>Handwritten card or personal note.</summary>
    HandwrittenNote = 5,
}
