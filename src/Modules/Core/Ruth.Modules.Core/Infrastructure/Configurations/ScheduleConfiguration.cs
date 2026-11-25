using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Schedules;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        _ = builder.ToTable("schedules");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50);
        _ = builder
            .Property(x => x.AbbreviatedName)
            .HasColumnName("abbreviated_name")
            .HasMaxLength(50);
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.ICalendarContent).HasColumnName("icalendar_content");
        _ = builder
            .Property(x => x.CheckInStartOffsetMinutes)
            .HasColumnName("check_in_start_offset_minutes");
        _ = builder
            .Property(x => x.CheckInEndOffsetMinutes)
            .HasColumnName("check_in_end_offset_minutes");
        _ = builder.Property(x => x.EffectiveStartDate).HasColumnName("effective_start_date");
        _ = builder.Property(x => x.EffectiveEndDate).HasColumnName("effective_end_date");
        _ = builder
            .Property(x => x.WeeklyDayOfWeek)
            .HasColumnName("weekly_day_of_week")
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder.Property(x => x.WeeklyTimeOfDay).HasColumnName("weekly_time_of_day");
        _ = builder.Property(x => x.CategoryId).HasColumnName("category_id");
        _ = builder.HasIndex(x => x.CategoryId);
        _ = builder
            .Property(x => x.AutoInactivateWhenComplete)
            .HasColumnName("auto_inactivate_when_complete")
            .IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.IsPublic).HasColumnName("is_public").IsRequired();
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.Ignore(x => x.IsCheckInEnabled);
        _ = builder.Ignore(x => x.EntityTypeKey);
        _ = builder.Ignore(x => x.EntityId);
        _ = builder.Ignore(x => x.ParentAuthority);
    }
}
