using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningActivityConfiguration : IEntityTypeConfiguration<LearningActivity>
{
    public void Configure(EntityTypeBuilder<LearningActivity> builder)
    {
        _ = builder.ToTable("learning_activities");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder.Property(static x => x.ComponentType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.AssignTo).HasConversion<string>().HasMaxLength(24);
        _ = builder
            .Property(static x => x.AvailabilityCriteria)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder
            .Property(static x => x.DueDateCriteria)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static x => x.ActivityConfigJson).HasColumnType("jsonb");
        _ = builder.HasIndex(static x => new { x.LearningClassId, x.Order });
    }
}
