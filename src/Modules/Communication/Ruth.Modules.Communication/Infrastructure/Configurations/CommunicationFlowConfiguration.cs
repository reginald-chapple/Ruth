using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.Flows;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationFlowConfiguration : IEntityTypeConfiguration<CommunicationFlow>
{
    private static readonly ValueComparer<List<CommunicationFlowStep>> _stepsComparer = new(
        static (left, right) => EqualsSteps(left, right),
        static list => StringComparer.Ordinal.GetHashCode(SerializeSteps(list)),
        static list => DeserializeSteps(SerializeSteps(list))
    );

    public void Configure(EntityTypeBuilder<CommunicationFlow> builder)
    {
        _ = builder.ToTable("communication_flows");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(150).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(100).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1_000);
        _ = builder
            .Property(static x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.TopicKey).HasMaxLength(80).IsRequired();
        _ = builder
            .Property(static x => x.TriggerMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder
            .Property(static x => x.ExitCondition)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder
            .Property(static x => x.ConversionGoalKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.ConversionGoalUrlPrefix).HasMaxLength(500);

        _ = builder
            .Property<List<CommunicationFlowStep>>("_steps")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("steps")
            .HasColumnType("jsonb")
            .HasConversion(
                static list => SerializeSteps(list),
                static json => DeserializeSteps(json),
                _stepsComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.Steps);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);

        _ = builder.HasIndex(static x => x.Key).IsUnique();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
        _ = builder.HasIndex(static x => new { x.Category, x.IsActive });
    }

    private static string SerializeSteps(List<CommunicationFlowStep>? list)
    {
        return JsonSerializer.Serialize(list ?? [], CommunicationTemplateConfiguration.JsonOptions);
    }

    private static List<CommunicationFlowStep> DeserializeSteps(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<CommunicationFlowStep>>(
                json,
                CommunicationTemplateConfiguration.JsonOptions
            ) ?? [];
    }

    private static bool EqualsSteps(
        List<CommunicationFlowStep>? left,
        List<CommunicationFlowStep>? right
    )
    {
        return ReferenceEquals(left, right)
            || string.Equals(SerializeSteps(left), SerializeSteps(right), StringComparison.Ordinal);
    }
}
