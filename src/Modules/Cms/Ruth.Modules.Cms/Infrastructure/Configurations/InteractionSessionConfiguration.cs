using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class InteractionSessionConfiguration : IEntityTypeConfiguration<InteractionSession>
{
    public void Configure(EntityTypeBuilder<InteractionSession> builder)
    {
        _ = builder.ToTable("interaction_sessions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.SessionKey).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.DeviceType).HasMaxLength(40);
        _ = builder.Property(static x => x.Browser).HasMaxLength(80);
        _ = builder.Property(static x => x.OperatingSystem).HasMaxLength(80);
        _ = builder.Property(static x => x.IpAddressHash).HasMaxLength(128);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.SessionKey).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
