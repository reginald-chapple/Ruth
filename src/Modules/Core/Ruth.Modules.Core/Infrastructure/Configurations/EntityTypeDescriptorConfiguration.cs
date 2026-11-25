using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class EntityTypeDescriptorConfiguration
    : IEntityTypeConfiguration<EntityTypeDescriptor>
{
    public void Configure(EntityTypeBuilder<EntityTypeDescriptor> builder)
    {
        _ = builder.ToTable("entity_type_descriptors");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        _ = builder.HasIndex(x => x.Key).IsUnique();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        _ = builder
            .Property(x => x.FriendlyName)
            .HasColumnName("friendly_name")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder
            .Property(x => x.ModuleName)
            .HasColumnName("module_name")
            .HasMaxLength(100)
            .IsRequired();
        _ = builder.Property(x => x.IsEntity).HasColumnName("is_entity").IsRequired();
        _ = builder.Property(x => x.IsSecured).HasColumnName("is_secured").IsRequired();
        _ = builder
            .Property(x => x.SupportsAttributes)
            .HasColumnName("supports_attributes")
            .IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
