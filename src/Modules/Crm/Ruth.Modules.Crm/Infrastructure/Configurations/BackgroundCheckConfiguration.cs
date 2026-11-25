using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.BackgroundChecks;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class BackgroundCheckConfiguration : IEntityTypeConfiguration<BackgroundCheck>
{
    public void Configure(EntityTypeBuilder<BackgroundCheck> builder)
    {
        _ = builder.ToTable("background_checks");
        _ = builder.HasKey(static b => b.Id);

        _ = builder.Property(static b => b.ProviderKey).HasMaxLength(64).IsRequired();
        _ = builder.Property(static b => b.PackageName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static b => b.RequestId).HasMaxLength(100);
        _ = builder.Property(static b => b.ResponseId).HasMaxLength(100);
        _ = builder.Property(static b => b.Status).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static b => b.RawProviderStatus).HasMaxLength(64).IsRequired();
        _ = builder.Property(static b => b.Recommendation).HasMaxLength(100).IsRequired();
        _ = builder.Property(static b => b.ResponseDataJson).HasColumnType("jsonb");

        _ = builder.HasIndex(static b => b.PersonId);
        _ = builder.HasIndex(static b => new { b.ProviderKey, b.RequestId });
    }
}
