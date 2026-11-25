using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Assessments;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class AssessmentTypeConfiguration : IEntityTypeConfiguration<AssessmentType>
{
    public void Configure(EntityTypeBuilder<AssessmentType> builder)
    {
        _ = builder.ToTable("assessment_types");
        _ = builder.HasKey(static t => t.Id);

        _ = builder.Property(static t => t.Key).HasMaxLength(100).IsRequired();
        _ = builder.Property(static t => t.NormalizedKey).HasMaxLength(100).IsRequired();
        _ = builder.Property(static t => t.Title).HasMaxLength(100).IsRequired();
        _ = builder.Property(static t => t.Description).HasMaxLength(2000);
        _ = builder.Property(static t => t.AssessmentPath).HasMaxLength(250).IsRequired();
        _ = builder.Property(static t => t.AssessmentResultsPath).HasMaxLength(250);
        _ = builder.Property(static t => t.IconName).HasMaxLength(100);
        _ = builder.Property(static t => t.BadgeColor).HasMaxLength(20);

        _ = builder.HasIndex(static t => t.NormalizedKey).IsUnique();
    }
}
