using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.BulkCommunications;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationSubscriptionPreferenceConfiguration
    : IEntityTypeConfiguration<CommunicationSubscriptionPreference>
{
    private static readonly ValueComparer<HashSet<string>> _topicsComparer = new(
        static (left, right) => EqualsTopics(left, right),
        static set => StringComparer.Ordinal.GetHashCode(SerializeTopics(set)),
        static set => DeserializeTopics(SerializeTopics(set))
    );

    public void Configure(EntityTypeBuilder<CommunicationSubscriptionPreference> builder)
    {
        _ = builder.ToTable("communication_subscription_preferences");
        _ = builder.HasKey(static x => x.Id);

        _ = builder
            .Property(static x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.NormalizedDestination).HasMaxLength(320).IsRequired();
        _ = builder.Property(static x => x.Reason).HasMaxLength(300);

        _ = builder
            .Property<HashSet<string>>("_optedOutTopics")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("opted_out_topics")
            .HasColumnType("jsonb")
            .HasConversion(
                static set => SerializeTopics(set),
                static json => DeserializeTopics(json),
                _topicsComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.OptedOutTopics);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.Channel, x.NormalizedDestination }).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
    }

    private static string SerializeTopics(HashSet<string>? set)
    {
        string[] ordered = set is null ? [] : [.. set.Order(StringComparer.Ordinal)];
        return JsonSerializer.Serialize(ordered, CommunicationTemplateConfiguration.JsonOptions);
    }

    private static HashSet<string> DeserializeTopics(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var items = JsonSerializer.Deserialize<string[]>(
            json,
            CommunicationTemplateConfiguration.JsonOptions
        );
        return items is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
    }

    private static bool EqualsTopics(HashSet<string>? left, HashSet<string>? right)
    {
        return ReferenceEquals(left, right)
            || string.Equals(
                SerializeTopics(left),
                SerializeTopics(right),
                StringComparison.Ordinal
            );
    }
}
