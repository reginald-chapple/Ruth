using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal static class CommerceModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyW4S9Configurations(modelBuilder);
        ApplyW4S10Configurations(modelBuilder);
    }

    private static void ApplyW4S9Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new PunchPassTypeEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ProductEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ProductVariantEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MemberPricingRuleEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommerceOrderEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CommerceOrderLineEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new IssuedPunchPassEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PunchPassLedgerEntryEntityConfiguration());
    }

    private static void ApplyW4S10Configurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new BookableServiceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RentableResourceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AvailabilityRuleEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BookingSlotHoldEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ServiceOrResourceBookingEntityConfiguration());
    }
}
