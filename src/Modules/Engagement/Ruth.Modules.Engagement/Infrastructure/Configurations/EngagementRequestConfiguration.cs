using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class EngagementRequestConfiguration : IEntityTypeConfiguration<EngagementRequest>
{
    public void Configure(EntityTypeBuilder<EngagementRequest> builder)
    {
        _ = builder.ToTable("engagement_requests");
        _ = builder.HasKey(static x => x.Id);
        _ = builder
            .Property(static x => x.RequestState)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.Comments).HasMaxLength(2000);

        _ = builder
            .HasMany(static x => x.Activities)
            .WithOne()
            .HasForeignKey(static a => a.EngagementRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Activities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.PipelineTypeId, x.RequestState });
        _ = builder.HasIndex(static x => x.RequesterPersonId);
        _ = builder.HasIndex(static x => new { x.RequestState, x.FollowUpDate });
    }
}
