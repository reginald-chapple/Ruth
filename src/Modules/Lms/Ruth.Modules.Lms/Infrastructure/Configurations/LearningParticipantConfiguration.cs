using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningParticipantConfiguration
    : IEntityTypeConfiguration<LearningParticipant>
{
    public void Configure(EntityTypeBuilder<LearningParticipant> builder)
    {
        _ = builder.ToTable("learning_participants");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Role).HasConversion<string>().HasMaxLength(24);
        _ = builder
            .Property(static x => x.LearningCompletionStatus)
            .HasConversion<string>()
            .HasMaxLength(24);
        _ = builder.Property(static x => x.CurrentGradePercent).HasPrecision(5, 2);
        _ = builder.Property(static x => x.CurrentGradeScaleName).HasMaxLength(64);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.LearningClassId, x.PersonId }).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
