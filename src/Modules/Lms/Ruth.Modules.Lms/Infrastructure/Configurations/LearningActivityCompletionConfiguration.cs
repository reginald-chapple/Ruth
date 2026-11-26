using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Classes;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningActivityCompletionConfiguration
    : IEntityTypeConfiguration<LearningActivityCompletion>
{
    public void Configure(EntityTypeBuilder<LearningActivityCompletion> builder)
    {
        _ = builder.ToTable("learning_activity_completions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.StudentComment).HasMaxLength(2000);
        _ = builder.Property(static x => x.FacilitatorComment).HasMaxLength(2000);
        _ = builder.Property(static x => x.SubmissionDataJson).HasColumnType("jsonb");
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder
            .HasIndex(static x => new { x.LearningParticipantId, x.LearningActivityId })
            .IsUnique();
        _ = builder.HasIndex(static x => new { x.LearningClassId, x.RequiresGrading });
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
