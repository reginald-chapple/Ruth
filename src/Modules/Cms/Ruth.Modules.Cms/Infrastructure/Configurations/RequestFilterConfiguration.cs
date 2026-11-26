using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class RequestFilterConfiguration : IEntityTypeConfiguration<RequestFilter>
{
    public void Configure(EntityTypeBuilder<RequestFilter> builder)
    {
        _ = builder.ToTable("request_filters");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.FilterMode).HasConversion<string>().HasMaxLength(24);
        CmsJsonbConverters.ConfigureJsonbList(builder, static x => x.Rules, "rules");
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Key).IsUnique();
    }
}
