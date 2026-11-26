using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.PhoneNumbers;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class SystemPhoneNumberConfiguration : IEntityTypeConfiguration<SystemPhoneNumber>
{
    public void Configure(EntityTypeBuilder<SystemPhoneNumber> builder)
    {
        _ = builder.ToTable("system_phone_numbers");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Number).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(500);
        _ = builder.Property(static x => x.ProviderIdentifier).HasMaxLength(120);
        _ = builder.Property(static x => x.IsSmsEnabled).IsRequired();
        _ = builder.Property(static x => x.IsSmsForwardingEnabled).IsRequired();
        _ = builder.Property(static x => x.IsActive).IsRequired();

        _ = builder.HasIndex(static x => x.Number).IsUnique();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
        _ = builder.HasIndex(static x => x.AssignedToPersonId);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);
    }
}
