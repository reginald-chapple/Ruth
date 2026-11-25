using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class PersonPreferenceConfiguration : IEntityTypeConfiguration<PersonPreference>
{
    public void Configure(EntityTypeBuilder<PersonPreference> builder)
    {
        _ = builder.ToTable("person_preferences");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.OwnerKey).HasColumnName("owner_key").IsRequired();
        _ = builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(250).IsRequired();
        _ = builder.HasIndex(x => new { x.OwnerKey, x.Key }).IsUnique();
        _ = builder.Property(x => x.EntityTypeId).HasColumnName("entity_type_id");
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id");
        _ = builder.HasIndex(x => new
        {
            x.OwnerKey,
            x.EntityTypeId,
            x.EntityId,
        });
        _ = builder.Property(x => x.Value).HasColumnName("value").IsRequired();
        _ = builder.Property(x => x.IsEnduring).HasColumnName("is_enduring").IsRequired();
        _ = builder
            .Property(x => x.LastAccessedOnUtc)
            .HasColumnName("last_accessed_on_utc")
            .IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
    }
}
