using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        _ = builder.ToTable("group_members");
        _ = builder.HasKey(static m => m.Id);
        _ = builder.Property(static m => m.Id).ValueGeneratedNever();

        _ = builder.Property(static m => m.Status).HasConversion<string>().HasMaxLength(16);
        _ = builder
            .Property(static m => m.CommunicationPreference)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static m => m.Note).HasMaxLength(2000);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static m => m.Attributes,
            "attributes",
            createGinIndex: true
        );
        _ = builder.Ignore(static m => m.AttributeValues);
        _ = builder.Ignore(static m => m.EntityTypeKey);
        _ = builder.Ignore(static m => m.EntityId);
        _ = builder.Ignore(static m => m.ParentAuthority);

        _ = builder.HasIndex(static m => new
        {
            m.GroupId,
            m.Status,
            m.IsArchived,
        });
        _ = builder.HasIndex(static m => m.PersonId);
        _ = builder
            .HasIndex(static m => new
            {
                m.GroupId,
                m.PersonId,
                m.GroupRoleId,
            })
            .HasFilter("\"IsArchived\" = false")
            .IsUnique();
    }
}
