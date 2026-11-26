namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Selection mode for an optional or required registration fee group.</summary>
public enum RegistrationFeeType
{
    /// <summary>Registrant selects at most one option item.</summary>
    SingleOption = 0,

    /// <summary>Registrant may select multiple option items and quantities.</summary>
    MultipleItems = 1,
}
