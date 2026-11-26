using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Streaks;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class StreakTypeConfiguration : IEntityTypeConfiguration<StreakType>
{
    public void Configure(EntityTypeBuilder<StreakType> builder)
    {
        _ = builder.ToTable("streak_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.Cadence)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        _ = builder
            .Property(static x => x.WeekEndDay)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
        _ = builder
            .Property(static x => x.SourceKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.Exclusions)
            .WithOne()
            .HasForeignKey(static e => e.StreakTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Exclusions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Code).IsUnique();
    }
}
