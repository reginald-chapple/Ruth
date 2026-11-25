using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class CampusTopicConfiguration : IEntityTypeConfiguration<CampusTopic>
{
    public void Configure(EntityTypeBuilder<CampusTopic> builder)
    {
        _ = builder.ToTable("campus_topics");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.CampusId).HasColumnName("campus_id").IsRequired();
        _ = builder
            .Property(x => x.TopicTypeValueId)
            .HasColumnName("topic_type_value_id")
            .IsRequired();
        _ = builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        _ = builder.Property(x => x.IsPublic).HasColumnName("is_public").IsRequired();

        _ = builder.HasIndex(x => x.CampusId);
    }
}
