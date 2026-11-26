using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationSessionEntityConfiguration
    : IEntityTypeConfiguration<RegistrationSession>
{
    public void Configure(EntityTypeBuilder<RegistrationSession> builder)
    {
        _ = builder.ToTable("registration_sessions");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.InstanceId).IsRequired();
        _ = builder.Property(static x => x.SessionToken).HasMaxLength(64).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.RegistrationId)
            .HasConversion(
                static id => id.HasValue ? id.Value.Value : (Guid?)null,
                static g => g.HasValue ? RegistrationId.From(g.Value) : null
            );

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.SessionToken).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.InstanceId,
            x.Status,
            x.ExpiresAtUtc,
        });
    }
}
