using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.Configurations;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class CheckInConfigurationEntityConfiguration
    : IEntityTypeConfiguration<CheckInConfiguration>
{
    public void Configure(EntityTypeBuilder<CheckInConfiguration> builder)
    {
        _ = builder.ToTable("check_in_configurations");
        _ = builder.HasKey(static c => c.Id);
        _ = builder.Property(static c => c.Id).ValueGeneratedNever();

        _ = builder.Property(static c => c.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static c => c.NormalizedName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static c => c.Description).HasMaxLength(2000);
        _ = builder.Property(static c => c.ProfileKind).HasConversion<string>().HasMaxLength(40);
        _ = builder.Property(static c => c.SearchMode).HasConversion<string>().HasMaxLength(32);

        _ = builder.Ignore(static c => c.EntityTypeKey);
        _ = builder.Ignore(static c => c.EntityId);
        _ = builder.Ignore(static c => c.ParentAuthority);

        _ = builder
            .HasMany(static c => c.Areas)
            .WithOne()
            .HasForeignKey(static a => a.CheckInConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static c => c.Areas).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static c => c.NormalizedName).IsUnique();
        _ = builder.HasIndex(static c => c.BranchId);
    }
}
