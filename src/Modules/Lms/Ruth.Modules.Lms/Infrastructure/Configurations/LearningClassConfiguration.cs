using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningClassConfiguration : IEntityTypeConfiguration<LearningClass>
{
    public void Configure(EntityTypeBuilder<LearningClass> builder)
    {
        _ = builder.ToTable("learning_classes");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder
            .HasMany(static x => x.ContentPages)
            .WithOne()
            .HasForeignKey(static p => p.LearningClassId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.ContentPages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .HasMany(static x => x.Announcements)
            .WithOne()
            .HasForeignKey(static a => a.LearningClassId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Announcements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .HasMany(static x => x.Activities)
            .WithOne()
            .HasForeignKey(static a => a.LearningClassId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.LearningCourseId);
    }
}
