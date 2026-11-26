namespace Ruth.Modules.Workflow.Contracts.Instances;

/// <summary>Classification of a WorkflowLog audit row.</summary>
public enum WorkflowLogKind
{
    /// <summary>Workflow lifecycle state transition.</summary>
    Lifecycle = 0,

    /// <summary>Activity activation, assignment, or completion.</summary>
    Activity = 1,

    /// <summary>Action plugin execution result.</summary>
    Action = 2,

    /// <summary>Interactive form submission event.</summary>
    FormSubmission = 3,

    /// <summary>Workflow or activity attribute mutation.</summary>
    AttributeChange = 4,

    /// <summary>Execution warning or error entry.</summary>
    Error = 5,
}
