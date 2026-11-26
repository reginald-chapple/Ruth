namespace Ruth.Modules.Workflow.Contracts.Actions;

/// <summary>Execution directive returned by an IWorkflowAction step.</summary>
public enum WorkflowActionOutcomeKind
{
    /// <summary>Marks this action completed and advances to the next step.</summary>
    Continue = 0,

    /// <summary>Leaves this action open waiting for external or interactive form input.</summary>
    WaitForInput = 1,

    /// <summary>Suspends processing until a scheduled UTC wake-up timestamp.</summary>
    DelayUntil = 2,

    /// <summary>Activates another WorkflowActivityType by key.</summary>
    ActivateActivity = 3,

    /// <summary>Completes the enclosing activity immediately.</summary>
    CompleteActivity = 4,

    /// <summary>Completes the entire workflow instance immediately.</summary>
    CompleteWorkflow = 5,

    /// <summary>Cancels the entire workflow instance immediately.</summary>
    CancelWorkflow = 6,
}
