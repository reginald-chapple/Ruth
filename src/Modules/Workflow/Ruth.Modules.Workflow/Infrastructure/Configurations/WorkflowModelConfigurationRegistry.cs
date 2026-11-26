using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal static class WorkflowModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new WorkflowTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActivityTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActionTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowInstanceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActivityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowLogConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowTriggerConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FormBuilderTemplateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActionFormConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActionFormSectionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WorkflowActionFormFieldConfiguration());
    }
}
