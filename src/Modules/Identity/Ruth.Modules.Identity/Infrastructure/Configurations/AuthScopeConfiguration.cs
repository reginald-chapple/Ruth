using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class AuthScopeConfiguration : IEntityTypeConfiguration<AuthScope>
{
    public void Configure(EntityTypeBuilder<AuthScope> builder)
    {
        _ = builder.ToTable("auth_scopes");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        _ = builder.HasIndex(x => x.Name).IsUnique();
        _ = builder
            .Property(x => x.PublicName)
            .HasColumnName("public_name")
            .HasMaxLength(100)
            .IsRequired();
        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
