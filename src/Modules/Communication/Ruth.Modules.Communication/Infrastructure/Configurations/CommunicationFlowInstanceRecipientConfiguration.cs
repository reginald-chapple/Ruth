using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.Flows;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationFlowInstanceRecipientConfiguration
    : IEntityTypeConfiguration<CommunicationFlowInstanceRecipient>
{
    private static readonly ValueComparer<Dictionary<string, string?>> _mergeComparer = new(
        static (left, right) => EqualsMerge(left, right),
        static dict => StringComparer.Ordinal.GetHashCode(SerializeMerge(dict)),
        static dict => DeserializeMerge(SerializeMerge(dict))
    );

    public void Configure(EntityTypeBuilder<CommunicationFlowInstanceRecipient> builder)
    {
        _ = builder.ToTable("communication_flow_instance_recipients");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FlowId).IsRequired();
        _ = builder.Property(static x => x.FlowInstanceId).IsRequired();
        _ = builder.Property(static x => x.EmailAddress).HasMaxLength(320);
        _ = builder.Property(static x => x.NormalizedEmail).HasMaxLength(320);
        _ = builder.Property(static x => x.PhoneNumber).HasMaxLength(24);
        _ = builder.Property(static x => x.DisplayName).HasMaxLength(200);
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.ExitReason).HasMaxLength(300);

        _ = builder
            .Property<Dictionary<string, string?>>("_mergeFields")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("merge_fields")
            .HasColumnType("jsonb")
            .HasConversion(
                static dict => SerializeMerge(dict),
                static json => DeserializeMerge(json),
                _mergeComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.MergeFields);

        _ = builder.HasIndex(static x => new { x.Status, x.NextStepDueAtUtc });
        _ = builder.HasIndex(static x => new { x.FlowId, x.Status });
        _ = builder.HasIndex(static x => x.FlowInstanceId);
        _ = builder.HasIndex(static x => x.PersonId);
    }

    private static string SerializeMerge(Dictionary<string, string?>? dict)
    {
        return JsonSerializer.Serialize(
            dict ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            CommunicationTemplateConfiguration.JsonOptions
        );
    }

    private static Dictionary<string, string?> DeserializeMerge(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, string?>>(
            json,
            CommunicationTemplateConfiguration.JsonOptions
        );
        return raw is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(raw, StringComparer.OrdinalIgnoreCase);
    }

    private static bool EqualsMerge(
        Dictionary<string, string?>? left,
        Dictionary<string, string?>? right
    )
    {
        return ReferenceEquals(left, right)
            || string.Equals(SerializeMerge(left), SerializeMerge(right), StringComparison.Ordinal);
    }
}
