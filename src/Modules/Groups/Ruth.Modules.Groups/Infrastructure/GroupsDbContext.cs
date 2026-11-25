using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.Modules.Groups.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Groups.Infrastructure;

/// <summary>EF Core database context exclusively owning the groups PostgreSQL schema.</summary>
public sealed class GroupsDbContext(DbContextOptions<GroupsDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Groups module.</summary>
    public const string SchemaName = "groups";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the group types table.</summary>
    public DbSet<GroupType> GroupTypes => Set<GroupType>();

    /// <summary>Gets the group type roles table.</summary>
    public DbSet<GroupTypeRole> GroupTypeRoles => Set<GroupTypeRole>();

    /// <summary>Gets the allowed child group types join table.</summary>
    public DbSet<GroupTypeAllowedChild> GroupTypeAllowedChildren => Set<GroupTypeAllowedChild>();

    /// <summary>Gets the groups table.</summary>
    public DbSet<Group> Groups => Set<Group>();

    /// <summary>Gets the group locations table.</summary>
    public DbSet<GroupLocation> GroupLocations => Set<GroupLocation>();

    /// <summary>Gets the group demographic types table.</summary>
    public DbSet<GroupDemographicType> GroupDemographicTypes => Set<GroupDemographicType>();

    /// <summary>Gets the group demographic values table.</summary>
    public DbSet<GroupDemographicValue> GroupDemographicValues => Set<GroupDemographicValue>();

    /// <summary>Gets the group historical snapshots table.</summary>
    public DbSet<GroupHistorical> GroupHistoricals => Set<GroupHistorical>();

    /// <summary>Gets the group members table.</summary>
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    /// <summary>Gets the group member historical snapshots table.</summary>
    public DbSet<GroupMemberHistorical> GroupMemberHistoricals => Set<GroupMemberHistorical>();

    /// <summary>Gets the group requirement types table.</summary>
    public DbSet<GroupRequirementType> GroupRequirementTypes => Set<GroupRequirementType>();

    /// <summary>Gets the group requirements table.</summary>
    public DbSet<GroupRequirement> GroupRequirements => Set<GroupRequirement>();

    /// <summary>Gets the group member requirements table.</summary>
    public DbSet<GroupMemberRequirement> GroupMemberRequirements => Set<GroupMemberRequirement>();

    /// <summary>Gets the group sync rules table.</summary>
    public DbSet<GroupSyncRule> GroupSyncRules => Set<GroupSyncRule>();

    /// <summary>Gets the group member schedule templates table.</summary>
    public DbSet<GroupMemberScheduleTemplate> ScheduleTemplates =>
        Set<GroupMemberScheduleTemplate>();

    /// <summary>Gets the group location schedule configs table.</summary>
    public DbSet<GroupLocationScheduleConfig> LocationScheduleConfigs =>
        Set<GroupLocationScheduleConfig>();

    /// <summary>Gets the group member standing assignments table.</summary>
    public DbSet<GroupMemberAssignment> GroupMemberAssignments => Set<GroupMemberAssignment>();

    /// <summary>Gets the personal blackout date exclusions table.</summary>
    public DbSet<PersonScheduleExclusion> PersonScheduleExclusions =>
        Set<PersonScheduleExclusion>();

    /// <summary>Gets the group type schedule exclusions table.</summary>
    public DbSet<GroupScheduleExclusion> GroupScheduleExclusions => Set<GroupScheduleExclusion>();

    /// <summary>Gets the attendance occurrences table.</summary>
    public DbSet<AttendanceOccurrence> AttendanceOccurrences => Set<AttendanceOccurrence>();

    /// <summary>Gets the attendance records table.</summary>
    public DbSet<Attendance> Attendances => Set<Attendance>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasPostgresExtension("postgis");
        GroupsModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
