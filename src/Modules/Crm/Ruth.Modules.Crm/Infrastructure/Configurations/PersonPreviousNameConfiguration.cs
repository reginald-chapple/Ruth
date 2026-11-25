using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonPreviousNameConfiguration : IEntityTypeConfiguration<PersonPreviousName>
{
    public void Configure(EntityTypeBuilder<PersonPreviousName> builder)
    {
        _ = builder.ToTable("person_previous_names");
        _ = builder.HasKey(static p => p.Id);

        _ = builder.Property(static p => p.LastName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NormalizedLastName).HasMaxLength(50).IsRequired();

        _ = builder.HasIndex(static p => new { p.PersonId, p.NormalizedLastName }).IsUnique();
        _ = builder.HasIndex(static p => p.NormalizedLastName);
    }
}
