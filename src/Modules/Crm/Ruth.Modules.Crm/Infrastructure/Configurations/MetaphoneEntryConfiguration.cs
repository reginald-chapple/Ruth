using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class MetaphoneEntryConfiguration : IEntityTypeConfiguration<MetaphoneEntry>
{
    public void Configure(EntityTypeBuilder<MetaphoneEntry> builder)
    {
        _ = builder.ToTable("metaphones");
        _ = builder.HasKey(static m => m.Name);

        _ = builder.Property(static m => m.Name).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.PrimaryKey).HasMaxLength(4).IsRequired();
        _ = builder.Property(static m => m.AlternateKey).HasMaxLength(4);

        _ = builder.HasIndex(static m => m.PrimaryKey);
        _ = builder.HasIndex(static m => m.AlternateKey);
    }
}
