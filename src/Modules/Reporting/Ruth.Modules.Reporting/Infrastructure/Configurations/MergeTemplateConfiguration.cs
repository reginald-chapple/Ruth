using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.MergeTemplates;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class MergeTemplateConfiguration : IEntityTypeConfiguration<MergeTemplate>
{
    public void Configure(EntityTypeBuilder<MergeTemplate> builder)
    {
        _ = builder.ToTable("merge_templates");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.Format).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static x => x.TemplateBody).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
