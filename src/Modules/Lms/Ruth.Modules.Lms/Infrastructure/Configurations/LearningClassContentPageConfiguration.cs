using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningClassContentPageConfiguration
    : IEntityTypeConfiguration<LearningClassContentPage>
{
    public void Configure(EntityTypeBuilder<LearningClassContentPage> builder)
    {
        _ = builder.ToTable("learning_class_content_pages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.ContentHtml).IsRequired();
        _ = builder.HasIndex(static x => new { x.LearningClassId, x.Order });
    }
}
