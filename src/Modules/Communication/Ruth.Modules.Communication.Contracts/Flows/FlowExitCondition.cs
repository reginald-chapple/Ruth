namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Early-exit rules evaluated before sending subsequent drip steps to a recipient.</summary>
public enum FlowExitCondition
{
    /// <summary>Deliver all configured steps unless the recipient unsubscribes.</summary>
    RunAllSteps = 0,

    /// <summary>Stop sending remaining steps once the recipient achieves the conversion goal.</summary>
    ExitOnConversion = 1,

    /// <summary>Stop sending remaining steps once the recipient converts or clicks a link.</summary>
    ExitOnConversionOrClick = 2,

    /// <summary>Stop sending remaining steps once the recipient opens, clicks, or converts.</summary>
    ExitOnAnyEngagement = 3,
}
