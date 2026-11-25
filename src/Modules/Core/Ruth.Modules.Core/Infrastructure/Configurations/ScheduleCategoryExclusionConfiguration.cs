using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain.Schedules;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class ScheduleCategoryExclusionConfiguration
    : IEntityTypeConfiguration<ScheduleCategoryExclusion>
{
    public void Configure(EntityTypeBuilder<ScheduleCategoryExclusion> builder)
    {
        _ = builder.ToTable("schedule_category_exclusions");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id").IsRequired();
        _ = builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(50).IsRequired();
        _ = builder.Property(x => x.StartDate).HasColumnName("start_date").IsRequired();
        _ = builder.Property(x => x.EndDate).HasColumnName("end_date").IsRequired();
        _ = builder.Property(x => x.StartOnUtc).HasColumnName("start_on_utc").IsRequired();
        _ = builder.Property(x => x.EndOnUtc).HasColumnName("end_on_utc").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder.HasIndex(x => new
        {
            x.CategoryId,
            x.StartOnUtc,
            x.EndOnUtc,
        });
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
