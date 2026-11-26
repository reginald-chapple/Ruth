using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Grading;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningGradingSystemConfiguration
    : IEntityTypeConfiguration<LearningGradingSystem>
{
    public void Configure(EntityTypeBuilder<LearningGradingSystem> builder)
    {
        _ = builder.ToTable("learning_grading_systems");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .HasMany(static x => x.Scales)
            .WithOne()
            .HasForeignKey(static s => s.LearningGradingSystemId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Scales)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
