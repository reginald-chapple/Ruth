using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupTypeConfiguration : IEntityTypeConfiguration<GroupType>
{
    public void Configure(EntityTypeBuilder<GroupType> builder)
    {
        _ = builder.ToTable("group_types");
        _ = builder.HasKey(static gt => gt.Id);
        _ = builder.Property(static gt => gt.Id).ValueGeneratedNever();

        _ = builder.Property(static gt => gt.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static gt => gt.Description).HasMaxLength(2000);
        _ = builder.Property(static gt => gt.GroupTerm).HasMaxLength(100).IsRequired();
        _ = builder.Property(static gt => gt.GroupMemberTerm).HasMaxLength(100).IsRequired();
        _ = builder.Property(static gt => gt.IconCssClass).HasMaxLength(100);
        _ = builder
            .Property(static gt => gt.AttendanceRule)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder
            .Property(static gt => gt.GroupCapacityRule)
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder
            .Property(static gt => gt.ScheduleConfirmationLogic)
            .HasConversion<string>()
            .HasMaxLength(16);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static gt => gt.Attributes,
            "attributes",
            createGinIndex: true
        );
        _ = builder.Ignore(static gt => gt.AttributeValues);
        _ = builder.Ignore(static gt => gt.EntityTypeKey);
        _ = builder.Ignore(static gt => gt.EntityId);
        _ = builder.Ignore(static gt => gt.ParentAuthority);

        _ = builder
            .HasMany(static gt => gt.Roles)
            .WithOne()
            .HasForeignKey(static r => r.GroupTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static gt => gt.AllowedChildren)
            .WithOne()
            .HasForeignKey(static c => c.ParentGroupTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static gt => gt.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static gt => gt.AllowedChildren)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static gt => gt.Name).IsUnique();
        _ = builder.HasIndex(static gt => gt.Order);
    }
}
