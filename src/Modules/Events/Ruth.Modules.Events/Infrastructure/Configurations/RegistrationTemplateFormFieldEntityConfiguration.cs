using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateFormFieldEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplateFormField>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplateFormField> builder)
    {
        _ = builder.ToTable("registration_template_form_fields");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FormId).IsRequired();
        _ = builder.Property(static x => x.FieldKey).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.Label).HasMaxLength(160).IsRequired();
        _ = builder
            .Property(static x => x.FieldKind)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.ShowWhenFieldKey).HasMaxLength(80);
        _ = builder.Property(static x => x.ShowWhenEqualsValue).HasMaxLength(160);

        EventsJsonbListConventions.ConfigureStringList(
            builder,
            static x => x.SelectOptions,
            columnName: "select_options"
        );

        _ = builder.HasIndex(static x => new { x.FormId, x.DisplayOrder });
    }
}
