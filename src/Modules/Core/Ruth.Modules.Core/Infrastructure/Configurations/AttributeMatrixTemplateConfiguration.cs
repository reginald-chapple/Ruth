using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal sealed class AttributeMatrixTemplateConfiguration
    : IEntityTypeConfiguration<AttributeMatrixTemplate>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<AttributeMatrixTemplate> builder)
    {
        _ = builder.ToTable("attribute_matrix_templates");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(x => x.SystemGuid).IsUnique();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(CoreTextPolicy.NameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(CoreTextPolicy.DescriptionMaxLength);
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.MinimumRows).HasColumnName("minimum_rows");
        _ = builder.Property(x => x.MaximumRows).HasColumnName("maximum_rows");
        _ = builder.Property(x => x.FormattedTemplate).HasColumnName("formatted_template");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        var converter = new ValueConverter<IReadOnlyList<MatrixColumnDefinition>, string>(
            static cols => JsonSerializer.Serialize(cols, _jsonOptions),
            static json => DeserializeColumns(json)
        );
        var comparer = new ValueComparer<IReadOnlyList<MatrixColumnDefinition>>(
            static (a, b) =>
                string.Equals(
                    JsonSerializer.Serialize(a, _jsonOptions),
                    JsonSerializer.Serialize(b, _jsonOptions),
                    StringComparison.Ordinal
                ),
            static cols =>
                StringComparer.Ordinal.GetHashCode(JsonSerializer.Serialize(cols, _jsonOptions)),
            static cols => DeserializeColumns(JsonSerializer.Serialize(cols, _jsonOptions))
        );

        var prop = builder
            .Property(x => x.Columns)
            .HasColumnName("columns")
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        prop.Metadata.SetValueComparer(comparer);
    }

    private static List<MatrixColumnDefinition> DeserializeColumns(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<MatrixColumnDefinition>>(json, _jsonOptions) ?? [];
    }
}
