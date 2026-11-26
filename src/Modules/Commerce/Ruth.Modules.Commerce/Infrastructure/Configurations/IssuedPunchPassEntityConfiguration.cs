using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.PunchPasses;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class IssuedPunchPassEntityConfiguration : IEntityTypeConfiguration<IssuedPunchPass>
{
    public void Configure(EntityTypeBuilder<IssuedPunchPass> builder)
    {
        _ = builder.ToTable("issued_punch_passes");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.PassCode).HasMaxLength(32).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.LedgerEntries)
            .WithOne()
            .HasForeignKey(static x => x.IssuedPunchPassId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.LedgerEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.PassCode).IsUnique();
        _ = builder.HasIndex(static x => new { x.OwnerPersonId, x.Status });
    }
}
