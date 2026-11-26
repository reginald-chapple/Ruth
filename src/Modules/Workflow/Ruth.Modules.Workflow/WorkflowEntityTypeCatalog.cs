using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Domain.Triggers;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Workflow;

internal static class WorkflowEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            WorkflowSystemGuids.WorkflowType,
            WorkflowType.EntityTypeKeyName,
            nameof(WorkflowType),
            "Workflow Type",
            "Workflow",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            WorkflowSystemGuids.WorkflowInstance,
            WorkflowInstance.EntityTypeKeyName,
            nameof(WorkflowInstance),
            "Workflow Instance",
            "Workflow",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            WorkflowSystemGuids.WorkflowTrigger,
            WorkflowTrigger.EntityTypeKeyName,
            nameof(WorkflowTrigger),
            "Workflow Automation Trigger",
            "Workflow",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            WorkflowSystemGuids.FormBuilderTemplate,
            FormBuilderTemplate.EntityTypeKeyName,
            nameof(FormBuilderTemplate),
            "Form Builder Template",
            "Workflow",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
