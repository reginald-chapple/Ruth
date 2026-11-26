using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class JourneyProgramConfiguration : IEntityTypeConfiguration<JourneyProgram>
{
    public void Configure(EntityTypeBuilder<JourneyProgram> builder)
    {
        _ = builder.ToTable("journey_programs");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder
            .Property(static x => x.DefaultListView)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.CompletionMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.Statuses)
            .WithOne()
            .HasForeignKey(static s => s.JourneyProgramId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Statuses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.MilestoneTypes)
            .WithOne()
            .HasForeignKey(static m => m.JourneyProgramId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.MilestoneTypes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
