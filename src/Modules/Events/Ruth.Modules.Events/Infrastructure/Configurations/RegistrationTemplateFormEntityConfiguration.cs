using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateFormEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplateForm>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplateForm> builder)
    {
        _ = builder.ToTable("registration_template_forms");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();

        _ = builder
            .HasMany(static x => x.Fields)
            .WithOne()
            .HasForeignKey(static x => x.FormId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Fields)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static x => new { x.TemplateId, x.DisplayOrder });
    }
}
