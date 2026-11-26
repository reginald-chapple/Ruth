using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.SupportRequests;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class SupportRequestCommentConfiguration
    : IEntityTypeConfiguration<SupportRequestComment>
{
    public void Configure(EntityTypeBuilder<SupportRequestComment> builder)
    {
        _ = builder.ToTable("support_request_comments");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.AuthorDisplayName).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Body).HasMaxLength(2000).IsRequired();
        _ = builder.HasIndex(static x => new { x.SupportRequestId, x.CreatedAtUtc });
    }
}
