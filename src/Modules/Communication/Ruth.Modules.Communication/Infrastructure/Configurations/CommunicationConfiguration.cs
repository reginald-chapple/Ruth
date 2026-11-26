using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.BulkCommunications;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationConfiguration
    : IEntityTypeConfiguration<Domain.BulkCommunications.Communication>
{
    public void Configure(EntityTypeBuilder<Domain.BulkCommunications.Communication> builder)
    {
        _ = builder.ToTable("communications");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Title).HasMaxLength(200).IsRequired();
        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.TopicKey).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.Subject).HasMaxLength(500);
        _ = builder.Property(static x => x.PreheaderText).HasMaxLength(250);
        _ = builder.Property(static x => x.FromName).HasMaxLength(120);
        _ = builder.Property(static x => x.FromEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.ReplyToEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.RazorLayoutKey).HasMaxLength(120);
        _ = builder.Property(static x => x.SmsBody).HasMaxLength(1_600);
        _ = builder.Property(static x => x.PushTitle).HasMaxLength(200);
        _ = builder.Property(static x => x.PushBody).HasMaxLength(1_000);
        _ = builder.Property(static x => x.PushOpenUrl).HasMaxLength(1_000);
        _ = builder.Property(static x => x.ApprovalNote).HasMaxLength(1_000);

        _ = builder
            .Property<List<EmailSection>>("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("sections")
            .HasColumnType("jsonb")
            .HasConversion(
                static list => CommunicationTemplateConfiguration.SerializeSections(list),
                static json => CommunicationTemplateConfiguration.DeserializeSections(json),
                CommunicationTemplateConfiguration.SectionsComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.Sections);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);

        _ = builder.HasIndex(static x => new { x.Status, x.ScheduledSendAtUtc });
        _ = builder.HasIndex(static x => x.CreatedOnUtc);
    }
}
