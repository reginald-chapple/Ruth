using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Tenancy.Domain;

namespace Ruth.Modules.Tenancy.Infrastructure.Configurations;

internal sealed class PlatformOperatorConfiguration : IEntityTypeConfiguration<PlatformOperator>
{
    public void Configure(EntityTypeBuilder<PlatformOperator> builder)
    {
        _ = builder.ToTable("platform_operators");
        _ = builder.HasKey(x => x.Id);
        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.EmailNormalized)
            .HasColumnName("email_normalized")
            .HasMaxLength(256)
            .IsRequired();
        _ = builder
            .Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder
            .Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsConcurrencyToken()
            .IsRequired();

        _ = builder.HasIndex(x => x.EmailNormalized).IsUnique();
        _ = builder.Ignore(x => x.DomainEvents);
    }
}
