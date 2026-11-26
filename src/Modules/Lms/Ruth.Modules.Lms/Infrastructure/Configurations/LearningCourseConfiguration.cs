using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Courses;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningCourseConfiguration : IEntityTypeConfiguration<LearningCourse>
{
    public void Configure(EntityTypeBuilder<LearningCourse> builder)
    {
        _ = builder.ToTable("learning_courses");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.PublicName).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.CourseCode).HasMaxLength(40).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder
            .HasMany(static x => x.Requirements)
            .WithOne()
            .HasForeignKey(static r => r.LearningCourseId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Requirements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.LearningProgramId, x.CourseCode }).IsUnique();
    }
}
