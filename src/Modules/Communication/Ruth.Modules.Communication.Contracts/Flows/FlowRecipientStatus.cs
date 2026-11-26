namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Progression state of an individual recipient inside a CommunicationFlowInstance.</summary>
public enum FlowRecipientStatus
{
    /// <summary>Eligible and awaiting the next scheduled step.</summary>
    Active = 0,

    /// <summary>Delivered all applicable steps in the sequence.</summary>
    Completed = 1,

    /// <summary>Exited early after achieving the conversion or engagement exit rule.</summary>
    ConvertedExited = 2,

    /// <summary>Exited because the recipient opted out or bounced.</summary>
    UnsubscribedExited = 3,

    /// <summary>Manually paused.</summary>
    Paused = 4,

    /// <summary>Halted due to unrecoverable delivery or missing destination failure.</summary>
    Failed = 5,
}
