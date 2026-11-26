using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationInstanceEntityConfiguration
    : IEntityTypeConfiguration<RegistrationInstance>
{
    public void Configure(EntityTypeBuilder<RegistrationInstance> builder)
    {
        _ = builder.ToTable("registration_instances");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.CostOverridePerRegistrant).HasPrecision(18, 2);
        _ = builder.Property(static x => x.DepositOverridePerRegistrant).HasPrecision(18, 2);
        _ = builder.Property(static x => x.ContactEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.AdditionalInstructions).HasMaxLength(2000);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new { x.TemplateId, x.IsActive });
        _ = builder.HasIndex(static x => x.BranchId);
    }
}
