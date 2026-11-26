using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.BulkCommunications;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationTemplateConfiguration
    : IEntityTypeConfiguration<CommunicationTemplate>
{
    internal static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    internal static readonly ValueComparer<List<EmailSection>> SectionsComparer = new(
        static (left, right) => EqualsSections(left, right),
        static list => ComputeSectionsHash(list),
        static list => DeserializeSections(SerializeSections(list))
    );

    public void Configure(EntityTypeBuilder<CommunicationTemplate> builder)
    {
        _ = builder.ToTable("communication_templates");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(100).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.TopicKey).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.SubjectTemplate).HasMaxLength(500);
        _ = builder.Property(static x => x.PreheaderText).HasMaxLength(250);
        _ = builder.Property(static x => x.FromName).HasMaxLength(120);
        _ = builder.Property(static x => x.FromEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.ReplyToEmail).HasMaxLength(254);
        _ = builder.Property(static x => x.RazorLayoutKey).HasMaxLength(120);
        _ = builder.Property(static x => x.SmsBodyTemplate).HasMaxLength(1_600);
        _ = builder.Property(static x => x.PushTitleTemplate).HasMaxLength(200);
        _ = builder.Property(static x => x.PushBodyTemplate).HasMaxLength(1_000);

        _ = builder
            .Property<List<EmailSection>>("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("sections")
            .HasColumnType("jsonb")
            .HasConversion(
                static list => SerializeSections(list),
                static json => DeserializeSections(json),
                SectionsComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.Sections);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);

        _ = builder.HasIndex(static x => x.Key).IsUnique();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
        _ = builder.HasIndex(static x => new { x.Channel, x.IsActive });
    }

    internal static string SerializeSections(List<EmailSection>? list)
    {
        return JsonSerializer.Serialize(list ?? [], JsonOptions);
    }

    internal static List<EmailSection> DeserializeSections(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<EmailSection>>(json, JsonOptions) ?? [];
    }

    private static bool EqualsSections(List<EmailSection>? left, List<EmailSection>? right)
    {
        return ReferenceEquals(left, right)
            || string.Equals(
                SerializeSections(left),
                SerializeSections(right),
                StringComparison.Ordinal
            );
    }

    private static int ComputeSectionsHash(List<EmailSection>? list)
    {
        return StringComparer.Ordinal.GetHashCode(SerializeSections(list));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter());
        return opts;
    }
}
