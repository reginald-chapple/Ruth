using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Domain.Triggers;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Workflow.Infrastructure;

/// <summary>EF Core database context exclusively owning the workflow PostgreSQL schema.</summary>
public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the Workflow module.</summary>
    public const string SchemaName = "workflow";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<WorkflowType> WorkflowTypes => Set<WorkflowType>();
    public DbSet<WorkflowActivityType> WorkflowActivityTypes => Set<WorkflowActivityType>();
    public DbSet<WorkflowActionType> WorkflowActionTypes => Set<WorkflowActionType>();
    public DbSet<WorkflowInstance> Workflows => Set<WorkflowInstance>();
    public DbSet<WorkflowActivity> WorkflowActivities => Set<WorkflowActivity>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<WorkflowLog> WorkflowLogs => Set<WorkflowLog>();
    public DbSet<WorkflowTrigger> WorkflowTriggers => Set<WorkflowTrigger>();
    public DbSet<FormBuilderTemplate> FormBuilderTemplates => Set<FormBuilderTemplate>();
    public DbSet<WorkflowActionForm> WorkflowActionForms => Set<WorkflowActionForm>();
    public DbSet<WorkflowActionFormSection> WorkflowActionFormSections =>
        Set<WorkflowActionFormSection>();
    public DbSet<WorkflowActionFormField> WorkflowActionFormFields =>
        Set<WorkflowActionFormField>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.WorkflowModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
