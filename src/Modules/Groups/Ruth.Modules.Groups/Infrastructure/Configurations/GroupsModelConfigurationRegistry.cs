using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal static class GroupsModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyGroupConfigurations(modelBuilder);
        ApplyMemberAndRequirementConfigurations(modelBuilder);
        ApplySchedulingAndAttendanceConfigurations(modelBuilder);
    }

    private static void ApplyGroupConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new GroupTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupTypeRoleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupTypeAllowedChildConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupLocationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupDemographicTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupDemographicValueConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupHistoricalConfiguration());
    }

    private static void ApplyMemberAndRequirementConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new GroupMemberConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupMemberHistoricalConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupRequirementTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupRequirementConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupMemberRequirementConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupSyncRuleConfiguration());
    }

    private static void ApplySchedulingAndAttendanceConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new GroupMemberScheduleTemplateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupLocationScheduleConfigConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupMemberAssignmentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonScheduleExclusionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GroupScheduleExclusionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AttendanceOccurrenceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AttendanceConfiguration());
    }
}
