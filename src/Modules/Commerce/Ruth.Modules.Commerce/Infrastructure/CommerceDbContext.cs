using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Domain.PunchPasses;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Commerce.Infrastructure;

/// <summary>EF Core database context exclusively owning the commerce PostgreSQL schema.</summary>
public sealed class CommerceDbContext(DbContextOptions<CommerceDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the Commerce module.</summary>
    public const string SchemaName = "commerce";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<PunchPassType> PunchPassTypes => Set<PunchPassType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<MemberPricingRule> MemberPricingRules => Set<MemberPricingRule>();
    public DbSet<CommerceOrder> Orders => Set<CommerceOrder>();
    public DbSet<CommerceOrderLine> OrderLines => Set<CommerceOrderLine>();
    public DbSet<IssuedPunchPass> IssuedPunchPasses => Set<IssuedPunchPass>();
    public DbSet<PunchPassLedgerEntry> PunchPassLedgerEntries => Set<PunchPassLedgerEntry>();

    public DbSet<BookableService> BookableServices => Set<BookableService>();
    public DbSet<RentableResource> RentableResources => Set<RentableResource>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<BookingSlotHold> BookingSlotHolds => Set<BookingSlotHold>();
    public DbSet<ServiceOrResourceBooking> ServiceBookings => Set<ServiceOrResourceBooking>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.CommerceModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
