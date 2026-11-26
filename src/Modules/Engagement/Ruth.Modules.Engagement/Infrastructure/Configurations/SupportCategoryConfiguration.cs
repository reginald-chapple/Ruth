using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.SupportRequests;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class SupportCategoryConfiguration : IEntityTypeConfiguration<SupportCategory>
{
    public void Configure(EntityTypeBuilder<SupportCategory> builder)
    {
        _ = builder.ToTable("support_categories");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
