using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Signals;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class SignalTypeConfiguration : IEntityTypeConfiguration<SignalType>
{
    public void Configure(EntityTypeBuilder<SignalType> builder)
    {
        _ = builder.ToTable("signal_types");
        _ = builder.HasKey(static s => s.Id);

        _ = builder.Property(static s => s.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static s => s.NormalizedName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static s => s.Description).HasMaxLength(2000);
        _ = builder.Property(static s => s.SignalColor).HasMaxLength(100).IsRequired();
        _ = builder.Property(static s => s.SignalIcon).HasMaxLength(100).IsRequired();

        _ = builder.HasIndex(static s => s.NormalizedName).IsUnique();
        _ = builder.HasIndex(static s => s.Order);
    }
}
