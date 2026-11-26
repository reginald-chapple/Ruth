using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateFeeEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplateFee>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplateFee> builder)
    {
        _ = builder.ToTable("registration_template_fees");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.FeeType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.Items)
            .WithOne()
            .HasForeignKey(static x => x.FeeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static x => new { x.TemplateId, x.DisplayOrder });
    }
}
