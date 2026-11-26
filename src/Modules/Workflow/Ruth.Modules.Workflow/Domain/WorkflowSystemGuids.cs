namespace Ruth.Modules.Workflow.Domain;

/// <summary>Deterministic compile-time SystemGuids for Workflow entity registrations.</summary>
public static class WorkflowSystemGuids
{
    public static readonly Guid WorkflowType = new(
        0x01966500,
        0x0001,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x05,
        0x01
    );

    public static readonly Guid WorkflowInstance = new(
        0x01966500,
        0x0002,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x05,
        0x02
    );

    public static readonly Guid WorkflowTrigger = new(
        0x01966500,
        0x0003,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x05,
        0x03
    );

    public static readonly Guid FormBuilderTemplate = new(
        0x01966500,
        0x0004,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05,
        0x06,
        0x01
    );
}
