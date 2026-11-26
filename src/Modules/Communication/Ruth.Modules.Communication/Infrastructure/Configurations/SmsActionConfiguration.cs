using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.TwoWaySms;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class SmsActionConfiguration : IEntityTypeConfiguration<SmsAction>
{
    public void Configure(EntityTypeBuilder<SmsAction> builder)
    {
        _ = builder.ToTable("sms_actions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Id).ValueGeneratedNever();

        _ = builder.Property(static x => x.SmsPipelineId).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Order).IsRequired();
        _ = builder
            .Property(static x => x.MatchMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.Keyword).HasMaxLength(80);
        _ = builder
            .Property(static x => x.ActionKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.ReplyTemplate).HasMaxLength(1_600);
        _ = builder.Property(static x => x.TargetSystemCommunicationKey).HasMaxLength(100);

        _ = builder.HasIndex(static x => new { x.SmsPipelineId, x.Order });
    }
}
