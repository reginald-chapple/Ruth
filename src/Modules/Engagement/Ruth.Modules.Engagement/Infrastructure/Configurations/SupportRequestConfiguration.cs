using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> builder)
    {
        _ = builder.ToTable("support_requests");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.FirstName).HasMaxLength(100).IsRequired();
        _ = builder.Property(static x => x.LastName).HasMaxLength(100);
        _ = builder.Property(static x => x.Email).HasMaxLength(254);
        _ = builder.Property(static x => x.RequestText).HasMaxLength(4000).IsRequired();
        _ = builder.Property(static x => x.AnswerOrOutcomeSummary).HasMaxLength(2000);
        _ = builder
            .Property(static x => x.VisibilityLevel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder
            .Property(static x => x.ModerationState)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.Comments)
            .WithOne()
            .HasForeignKey(static c => c.SupportRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new
        {
            x.ModerationState,
            x.VisibilityLevel,
            x.ExpiresAtUtc,
        });
        _ = builder.HasIndex(static x => x.SupportCategoryId);
    }
}
