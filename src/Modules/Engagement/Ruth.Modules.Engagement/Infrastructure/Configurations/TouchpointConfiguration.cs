using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Touchpoints;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class TouchpointConfiguration : IEntityTypeConfiguration<Touchpoint>
{
    public void Configure(EntityTypeBuilder<Touchpoint> builder)
    {
        _ = builder.ToTable("touchpoints");
        _ = builder.HasKey(static x => x.Id);
        _ = builder
            .Property(static x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.OutcomeSentiment)
            .HasConversion<string>()
            .HasMaxLength(24);
        _ = builder.Property(static x => x.Summary).HasMaxLength(2000);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new
        {
            x.OwnerPersonId,
            x.Status,
            x.ScheduledDate,
        });
        _ = builder.HasIndex(static x => x.ConstituentContactId);
    }
}
