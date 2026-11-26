using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Bookmarks;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PersonalLinkSectionConfiguration
    : IEntityTypeConfiguration<PersonalLinkSection>
{
    public void Configure(EntityTypeBuilder<PersonalLinkSection> builder)
    {
        _ = builder.ToTable("personal_link_sections");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder
            .HasMany(static x => x.Links)
            .WithOne()
            .HasForeignKey(static l => l.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Links).UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
