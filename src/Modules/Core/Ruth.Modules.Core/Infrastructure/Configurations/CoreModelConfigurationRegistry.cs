using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal static class CoreModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyFoundation(modelBuilder);
        ApplyLocationsAndSchedules(modelBuilder);
        CoreCollaborationConfigurationRegistry.ApplyAll(modelBuilder);
    }

    private static void ApplyFoundation(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new EntityTypeDescriptorConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DefinedTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DefinedValueConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AttributeDefinitionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AttributeMatrixTemplateConfiguration());
    }

    private static void ApplyLocationsAndSchedules(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new BranchConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BranchScheduleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BranchTopicConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LocationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DeviceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DeviceLocationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ScheduleDateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ScheduleCategoryExclusionConfiguration());
    }
}
