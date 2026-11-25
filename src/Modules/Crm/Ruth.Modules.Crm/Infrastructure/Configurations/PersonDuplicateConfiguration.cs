using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Duplicates;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonDuplicateConfiguration : IEntityTypeConfiguration<PersonDuplicate>
{
    public void Configure(EntityTypeBuilder<PersonDuplicate> builder)
    {
        _ = builder.ToTable("person_duplicates");
        _ = builder.HasKey(static d => d.Id);

        _ = builder.Property(static d => d.ConfidenceScore).HasPrecision(5, 2);
        _ = builder.Property(static d => d.ScoreDetail).HasMaxLength(500).IsRequired();

        _ = builder.HasIndex(static d => new { d.Person1Id, d.Person2Id }).IsUnique();
        _ = builder.HasIndex(static d => d.ConfidenceScore);
        _ = builder.HasIndex(static d => d.Person2Id);
    }
}
