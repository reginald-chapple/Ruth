using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplatePlacementEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplatePlacement>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplatePlacement> builder)
    {
        _ = builder.ToTable("registration_template_placements");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.GroupTypeId).IsRequired();

        _ = builder.HasIndex(static x => x.TemplateId);
    }
}
