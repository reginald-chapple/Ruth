using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.SystemCommunications;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class SystemCommunicationConfiguration
    : IEntityTypeConfiguration<SystemCommunication>
{
    public void Configure(EntityTypeBuilder<SystemCommunication> builder)
    {
        _ = builder.ToTable("system_communications");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Title).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(100).IsRequired();
        _ = builder
            .Property(static x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.FromName).HasMaxLength(120);
        _ = builder.Property(static x => x.FromEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.SubjectTemplate).HasMaxLength(500).IsRequired();
        _ = builder.Property(static x => x.HtmlBodyTemplate).IsRequired();
        _ = builder.Property(static x => x.RazorLayoutKey).HasMaxLength(120);
        _ = builder.Property(static x => x.SmsMessageTemplate).HasMaxLength(1_600);
        _ = builder.Property(static x => x.PushTitleTemplate).HasMaxLength(200);
        _ = builder.Property(static x => x.PushMessageTemplate).HasMaxLength(1_000);
        _ = builder.Property(static x => x.PushOpenUrlTemplate).HasMaxLength(1_000);
        _ = builder.Property(static x => x.IsSystem).IsRequired();
        _ = builder.Property(static x => x.IsActive).IsRequired();

        _ = builder.HasIndex(static x => x.Key).IsUnique();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
        _ = builder.HasIndex(static x => new { x.Category, x.IsActive });

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);
    }
}
