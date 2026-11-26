using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
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

    /// <summary>Gets the check-in sessions table.</summary>
    public DbSet<AttendanceCheckInSession> AttendanceCheckInSessions =>
        Set<AttendanceCheckInSession>();

    /// <summary>Gets the participant check-in attendance entries table.</summary>
    public DbSet<CheckInAttendanceEntry> CheckInAttendanceEntries => Set<CheckInAttendanceEntry>();

    /// <summary>Gets the daily collision-free security pickup code ledger table.</summary>
    public DbSet<DailySecurityPickupCode> DailySecurityPickupCodes =>
        Set<DailySecurityPickupCode>();

    /// <summary>Gets the check-in badge and receipt label definitions table.</summary>
    public DbSet<CheckInLabel> CheckInLabels => Set<CheckInLabel>();

    /// <summary>Gets the smart wallet pass templates table.</summary>
    public DbSet<WalletPassTemplate> WalletPassTemplates => Set<WalletPassTemplate>();

    /// <summary>Gets the issued smart wallet passes table.</summary>
    public DbSet<IssuedWalletPass> IssuedWalletPasses => Set<IssuedWalletPass>();

    /// <summary>Gets the Apple PassKit device push registrations table.</summary>
    public DbSet<WalletPassDeviceRegistration> WalletPassDeviceRegistrations =>
        Set<WalletPassDeviceRegistration>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        CheckInModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
