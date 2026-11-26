using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain.Labels;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class CheckInLabelEntityConfiguration : IEntityTypeConfiguration<CheckInLabel>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CheckInLabel> builder)
    {
        _ = builder.ToTable("check_in_labels");
        _ = builder.HasKey(static l => l.Id);
        _ = builder.Property(static l => l.Id).ValueGeneratedNever();

        _ = builder.Property(static l => l.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static l => l.NormalizedName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static l => l.Key).HasMaxLength(80).IsRequired();
        _ = builder.Property(static l => l.Description).HasMaxLength(2000);
        _ = builder.Property(static l => l.LabelKind).HasConversion<string>().HasMaxLength(40);
        _ = builder.Property(static l => l.CustomZplOverride).HasMaxLength(16000);

        var layoutComparer = new ValueComparer<CheckInLabelLayoutSchema>(
            static (left, right) => EqualsLayout(left, right),
            static layout => ComputeLayoutHash(layout),
            static layout => CloneLayout(layout)
        );

        _ = builder
            .Property(static l => l.Layout)
            .HasColumnName("layout")
            .HasColumnType("jsonb")
            .HasConversion(
                static layout => SerializeLayout(layout),
                static json => DeserializeLayout(json),
                layoutComparer
            )
            .IsRequired();

        _ = builder.Ignore(static l => l.EntityTypeKey);
        _ = builder.Ignore(static l => l.EntityId);
        _ = builder.Ignore(static l => l.ParentAuthority);

        _ = builder.HasIndex(static l => l.Key).IsUnique();
        _ = builder.HasIndex(static l => new { l.LabelKind, l.IsActive });
        _ = builder.HasIndex(static l => l.ConfigurationId);
    }

    private static string SerializeLayout(CheckInLabelLayoutSchema layout)
    {
        return JsonSerializer.Serialize(layout.ToDto(), _jsonOptions);
    }

    private static CheckInLabelLayoutSchema DeserializeLayout(string json)
    {
        var dto = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<CheckInLabelLayoutDto>(json, _jsonOptions);
        var parsed = CheckInLabelLayoutSchema.Create(dto, allowEmptyElements: true);
        return parsed.IsSuccess
            ? parsed.Value
            : CheckInLabelLayoutSchema
                .Create(CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto())
                .Value;
    }

    private static bool EqualsLayout(
        CheckInLabelLayoutSchema? left,
        CheckInLabelLayoutSchema? right
    )
    {
        return ReferenceEquals(left, right)
            || (
                left is not null
                && right is not null
                && string.Equals(
                    SerializeLayout(left),
                    SerializeLayout(right),
                    StringComparison.Ordinal
                )
            );
    }

    private static int ComputeLayoutHash(CheckInLabelLayoutSchema layout)
    {
        return StringComparer.Ordinal.GetHashCode(SerializeLayout(layout));
    }

    private static CheckInLabelLayoutSchema CloneLayout(CheckInLabelLayoutSchema layout)
    {
        return DeserializeLayout(SerializeLayout(layout));
    }
}
