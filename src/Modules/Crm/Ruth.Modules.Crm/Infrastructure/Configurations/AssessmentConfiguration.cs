using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Assessments;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        _ = builder.ToTable("assessments");
        _ = builder.HasKey(static a => a.Id);

        _ = builder.Property(static a => a.Status).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static a => a.ResultDataJson).HasColumnType("jsonb");
        _ = builder.Property(static a => a.SummaryText).HasMaxLength(250);

        _ = builder.HasIndex(static a => new { a.PersonId, a.AssessmentTypeId });
        _ = builder.HasIndex(static a => a.Status);
    }
}
