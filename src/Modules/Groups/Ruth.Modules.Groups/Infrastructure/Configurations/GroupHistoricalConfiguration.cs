using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Groups;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupHistoricalConfiguration : IEntityTypeConfiguration<GroupHistorical>
{
    public void Configure(EntityTypeBuilder<GroupHistorical> builder)
    {
        _ = builder.ToTable("group_historicals");
        _ = builder.HasKey(static h => h.Id);
        _ = builder.Property(static h => h.Id).ValueGeneratedNever();
        _ = builder.Property(static h => h.GroupName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static h => h.GroupTypeName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static h => h.Description).HasMaxLength(2000);
        _ = builder.HasIndex(static h => new { h.GroupId, h.CurrentRowIndicator });
        _ = builder.HasIndex(static h => new { h.GroupId, h.EffectiveDateTimeUtc });
    }
}
