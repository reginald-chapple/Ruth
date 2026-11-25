using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        _ = builder.ToTable("groups");
        _ = builder.HasKey(static g => g.Id);
        _ = builder.Property(static g => g.Id).ValueGeneratedNever();

        _ = builder.Property(static g => g.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static g => g.Description).HasMaxLength(2000);
        _ = builder.Property(static g => g.MeetingStyle).HasConversion<string>().HasMaxLength(16);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static g => g.Attributes,
            "attributes",
            createGinIndex: true
        );
        _ = builder.Ignore(static g => g.AttributeValues);
        _ = builder.Ignore(static g => g.EntityTypeKey);
        _ = builder.Ignore(static g => g.EntityId);
        _ = builder.Ignore(static g => g.ParentAuthority);

        _ = builder
            .HasMany(static g => g.Locations)
            .WithOne()
            .HasForeignKey(static l => l.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static g => g.Locations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static g => g.GroupTypeId);
        _ = builder.HasIndex(static g => g.ParentGroupId);
        _ = builder.HasIndex(static g => g.BranchId);
        _ = builder.HasIndex(static g => new
        {
            g.IsActive,
            g.IsArchived,
            g.IsPublic,
        });
    }
}
