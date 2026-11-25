namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Category of a phone number attached to a person.</summary>
public enum PhoneNumberType
{
    /// <summary>Mobile / cell phone number.</summary>
    Mobile = 0,

    /// <summary>Home landline number.</summary>
    Home = 1,

    /// <summary>Work / office number.</summary>
    Work = 2,

    /// <summary>Other phone number.</summary>
    Other = 3,
}
