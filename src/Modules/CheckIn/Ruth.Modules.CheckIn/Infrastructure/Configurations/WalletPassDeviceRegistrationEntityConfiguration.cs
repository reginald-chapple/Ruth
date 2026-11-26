using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.WalletPasses;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class WalletPassDeviceRegistrationEntityConfiguration
    : IEntityTypeConfiguration<WalletPassDeviceRegistration>
{
    public void Configure(EntityTypeBuilder<WalletPassDeviceRegistration> builder)
    {
        _ = builder.ToTable("wallet_pass_device_registrations");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.IssuedPassId).IsRequired();
        _ = builder.Property(static x => x.DeviceLibraryIdentifier).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PassTypeIdentifier).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.SerialNumber).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.PushToken).HasMaxLength(256).IsRequired();

        _ = builder
            .HasIndex(static x => new
            {
                x.DeviceLibraryIdentifier,
                x.PassTypeIdentifier,
                x.SerialNumber,
            })
            .IsUnique();
        _ = builder.HasIndex(static x => new { x.DeviceLibraryIdentifier, x.PassTypeIdentifier });
        _ = builder.HasIndex(static x => x.IssuedPassId);
    }
}
