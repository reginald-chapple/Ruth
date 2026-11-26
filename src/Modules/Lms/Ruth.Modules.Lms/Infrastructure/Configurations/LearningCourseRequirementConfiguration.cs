using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Courses;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningCourseRequirementConfiguration
    : IEntityTypeConfiguration<LearningCourseRequirement>
{
    public void Configure(EntityTypeBuilder<LearningCourseRequirement> builder)
    {
        _ = builder.ToTable("learning_course_requirements");
        _ = builder.HasKey(static x => x.Id);
        _ = builder
            .Property(static x => x.RequirementType)
            .HasConversion<string>()
            .HasMaxLength(24);
        _ = builder
            .HasIndex(static x => new { x.LearningCourseId, x.RequiredLearningCourseId })
            .IsUnique();
    }
}
