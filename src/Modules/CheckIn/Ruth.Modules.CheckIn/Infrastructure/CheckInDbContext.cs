using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.CheckIn.Infrastructure;

/// <summary>EF Core database context exclusively owning the checkin PostgreSQL schema.</summary>
public sealed class CheckInDbContext(DbContextOptions<CheckInDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the CheckIn module.</summary>
    public const string SchemaName = "checkin";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the check-in configuration profiles table.</summary>
    public DbSet<CheckInConfiguration> CheckInConfigurations => Set<CheckInConfiguration>();

    /// <summary>Gets the check-in areas table.</summary>
    public DbSet<CheckInArea> CheckInAreas => Set<CheckInArea>();

    /// <summary>Gets the check-in area room/location matrix table.</summary>
    public DbSet<CheckInAreaLocation> CheckInAreaLocations => Set<CheckInAreaLocation>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        CheckInModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
