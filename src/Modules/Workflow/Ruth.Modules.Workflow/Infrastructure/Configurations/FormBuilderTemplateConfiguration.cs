using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Workflow.Domain.Forms;

namespace Ruth.Modules.Workflow.Infrastructure.Configurations;

internal sealed class FormBuilderTemplateConfiguration
    : IEntityTypeConfiguration<FormBuilderTemplate>
{
    public void Configure(EntityTypeBuilder<FormBuilderTemplate> builder)
    {
        _ = builder.ToTable("form_builder_templates");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder.Property(static x => x.FormHeaderHtml).HasMaxLength(8000);
        _ = builder.Property(static x => x.FormFooterHtml).HasMaxLength(8000);
        _ = builder.Property(static x => x.ConfirmationTitle).HasMaxLength(250).IsRequired();
        _ = builder
            .Property(static x => x.ConfirmationMessageTemplate)
            .HasMaxLength(4000)
            .IsRequired();
        _ = builder.Property(static x => x.CompletionRedirectUrl).HasMaxLength(500);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
