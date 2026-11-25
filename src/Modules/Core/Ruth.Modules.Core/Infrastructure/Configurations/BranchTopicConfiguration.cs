using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class BranchTopicConfiguration : IEntityTypeConfiguration<BranchTopic>
{
    public void Configure(EntityTypeBuilder<BranchTopic> builder)
    {
        _ = builder.ToTable("branch_topics");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
        _ = builder
            .Property(x => x.TopicTypeValueId)
            .HasColumnName("topic_type_value_id")
            .IsRequired();
        _ = builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        _ = builder.Property(x => x.IsPublic).HasColumnName("is_public").IsRequired();

        _ = builder.HasIndex(x => x.BranchId);
    }
}
