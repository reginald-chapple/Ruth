using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain.Configurations;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class CheckInAreaEntityConfiguration : IEntityTypeConfiguration<CheckInArea>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CheckInArea> builder)
    {
        _ = builder.ToTable("check_in_areas");
        _ = builder.HasKey(static a => a.Id);
        _ = builder.Property(static a => a.Id).ValueGeneratedNever();

        _ = builder.Property(static a => a.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static a => a.NormalizedName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static a => a.Description).HasMaxLength(2000);
        _ = builder
            .Property(static a => a.BalancingStrategy)
            .HasConversion<string>()
            .HasMaxLength(32);

        var eligibilityComparer = new ValueComparer<CheckInEligibilityRule>(
            static (left, right) => EqualsEligibility(left, right),
            static rule => ComputeEligibilityHash(rule),
            static rule => CloneEligibility(rule)
        );

        _ = builder
            .Property(static a => a.Eligibility)
            .HasColumnName("eligibility")
            .HasColumnType("jsonb")
            .HasConversion(
                static rule => SerializeEligibility(rule),
                static json => DeserializeEligibility(json),
                eligibilityComparer
            )
            .IsRequired();

        _ = builder
            .HasMany(static a => a.Rooms)
            .WithOne()
            .HasForeignKey(static r => r.CheckInAreaId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static a => a.Rooms).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasIndex(static a => new { a.CheckInConfigurationId, a.NormalizedName })
            .IsUnique();
        _ = builder.HasIndex(static a => new { a.CheckInConfigurationId, a.Order });
    }

    private static string SerializeEligibility(CheckInEligibilityRule rule)
    {
        return JsonSerializer.Serialize(rule.ToDto(), _jsonOptions);
    }

    private static CheckInEligibilityRule DeserializeEligibility(string json)
    {
        var dto = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<CheckInEligibilityRuleDto>(json, _jsonOptions);
        return CheckInEligibilityRule.Create(dto).Value;
    }

    private static bool EqualsEligibility(
        CheckInEligibilityRule? left,
        CheckInEligibilityRule? right
    )
    {
        return ReferenceEquals(left, right)
            || (
                left is not null
                && right is not null
                && string.Equals(
                    SerializeEligibility(left),
                    SerializeEligibility(right),
                    StringComparison.Ordinal
                )
            );
    }

    private static int ComputeEligibilityHash(CheckInEligibilityRule rule)
    {
        return StringComparer.Ordinal.GetHashCode(SerializeEligibility(rule));
    }

    private static CheckInEligibilityRule CloneEligibility(CheckInEligibilityRule rule)
    {
        return DeserializeEligibility(SerializeEligibility(rule));
    }
}
