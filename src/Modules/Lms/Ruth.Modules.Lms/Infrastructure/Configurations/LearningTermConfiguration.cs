using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Lms.Domain.Programs;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal sealed class LearningTermConfiguration : IEntityTypeConfiguration<LearningTerm>
{
    public void Configure(EntityTypeBuilder<LearningTerm> builder)
    {
        _ = builder.ToTable("learning_terms");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.PublicName).HasMaxLength(160).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.LearningProgramId);
    }
}
