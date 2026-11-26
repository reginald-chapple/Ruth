using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Programs;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningProgramConfiguration : IEntityTypeConfiguration<LearningProgram>
{
    public void Configure(EntityTypeBuilder<LearningProgram> builder)
    {
        _ = builder.ToTable("learning_programs");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.PublicName).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder
            .Property(static x => x.ConfigurationMode)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static x => x.HighlightColor).HasMaxLength(32);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(80);
        _ = builder.Ignore(static x => x.DomainEvents);
    }
}
