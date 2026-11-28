using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.Transports;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationTransportConfigConfiguration
    : IEntityTypeConfiguration<CommunicationTransportConfig>
{
    public const string CredentialsEncryptionPurpose = "comms.transport.credentials";

    public void Configure(EntityTypeBuilder<CommunicationTransportConfig> builder)
    {
        _ = builder.ToTable("communication_transport_configs");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TransportKey).HasMaxLength(64).IsRequired();
        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.IsEnabled).IsRequired();
        _ = builder.Property(static x => x.DefaultFromAddress).HasMaxLength(254);
        _ = builder.Property(static x => x.DefaultFromName).HasMaxLength(120);

        TenantEncryptedValueConverters.ConfigureEncryptedJsonbStringMap(
            builder,
            static x => x.Credentials,
            columnName: "credentials",
            purpose: CredentialsEncryptionPurpose,
            encryptAllValues: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.TransportKey).IsUnique();
    }
}
