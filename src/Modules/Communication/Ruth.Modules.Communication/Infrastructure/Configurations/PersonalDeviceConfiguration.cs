using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.PersonalDevices;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class PersonalDeviceConfiguration : IEntityTypeConfiguration<PersonalDevice>
{
    public void Configure(EntityTypeBuilder<PersonalDevice> builder)
    {
        _ = builder.ToTable("personal_devices");
        _ = builder.HasKey(static x => x.Id);

        _ = builder
            .Property(static x => x.Platform)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.PushRegistrationToken).HasMaxLength(2_048).IsRequired();
        _ = builder.Property(static x => x.DeviceName).HasMaxLength(120);
        _ = builder.Property(static x => x.DeviceVersion).HasMaxLength(64);
        _ = builder.Property(static x => x.NotificationsEnabled).IsRequired();
        _ = builder.Property(static x => x.IsActive).IsRequired();
        _ = builder.Property(static x => x.LastSeenOnUtc).IsRequired();

        _ = builder.HasIndex(static x => x.PushRegistrationToken).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.PersonId,
            x.IsActive,
            x.NotificationsEnabled,
        });

        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
