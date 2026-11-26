using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.TwoWaySms;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class SmsPipelineConfiguration : IEntityTypeConfiguration<SmsPipeline>
{
    public void Configure(EntityTypeBuilder<SmsPipeline> builder)
    {
        _ = builder.ToTable("sms_pipelines");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1_000);
        _ = builder.Property(static x => x.PriorityOrder).IsRequired();
        _ = builder.Property(static x => x.IsSystem).IsRequired();
        _ = builder.Property(static x => x.IsActive).IsRequired();

        _ = builder
            .HasMany(static x => x.Actions)
            .WithOne()
            .HasForeignKey(static a => a.SmsPipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .Navigation(static x => x.Actions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);

        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
        _ = builder.HasIndex(static x => new { x.IsActive, x.PriorityOrder });
    }
}
