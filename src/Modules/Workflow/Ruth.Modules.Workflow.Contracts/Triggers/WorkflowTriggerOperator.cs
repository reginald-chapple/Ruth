namespace Ruth.Modules.Workflow.Contracts.Triggers;

/// <summary>Predicate comparison operator evaluated on trigger payload attributes.</summary>
public enum WorkflowTriggerOperator
{
    /// <summary>Unconditional match for the event key.</summary>
    Always = 0,

    /// <summary>Case-insensitive ordinal equality match.</summary>
    Equals = 1,

    /// <summary>Case-insensitive ordinal inequality match.</summary>
    NotEquals = 2,

    /// <summary>Case-insensitive substring match.</summary>
    Contains = 3,

    /// <summary>Requires a non-whitespace payload value.</summary>
    IsNotEmpty = 4,

    /// <summary>Numeric decimal greater-than comparison.</summary>
    GreaterThan = 5,
}
