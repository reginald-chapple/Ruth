using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Catalog;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class PunchPassTypeEntityConfiguration : IEntityTypeConfiguration<PunchPassType>
{
    public void Configure(EntityTypeBuilder<PunchPassType> builder)
    {
        _ = builder.ToTable("punch_pass_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Code).HasMaxLength(48).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Code).IsUnique();
    }
}
