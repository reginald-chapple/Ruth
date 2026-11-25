namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Email communication preference for a person.</summary>
public enum EmailPreference
{
    /// <summary>All transactional and bulk email allowed.</summary>
    EmailAllowed = 0,

    /// <summary>Transactional email allowed; exclude from bulk/mass communications.</summary>
    NoMassEmails = 1,

    /// <summary>Do not send any email.</summary>
    DoNotEmail = 2,
}
