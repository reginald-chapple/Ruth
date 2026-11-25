using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ruth.Modules.Communication.Domain.Templates;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class TemplateSnippetConfiguration : IEntityTypeConfiguration<TemplateSnippet>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<TemplateSnippet> builder)
    {
        _ = builder.ToTable("template_snippets");
        _ = builder.HasKey(static x => x.Id);

        ConfigureScalarColumns(builder);
        ConfigureJsonColumns(builder);
        ConfigureAuditAndIgnoredColumns(builder);
    }

    private static void ConfigureScalarColumns(EntityTypeBuilder<TemplateSnippet> builder)
    {
        _ = builder.Property(static x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(static x => x.SystemGuid).HasColumnName("system_guid").IsRequired();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();

        _ = builder
            .Property(static x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(TemplateSnippet.NameMaxLength)
            .IsRequired();

        _ = builder
            .Property(static x => x.Key)
            .HasColumnName("key")
            .HasMaxLength(TemplateSnippet.KeyMaxLength)
            .IsRequired();
        _ = builder.HasIndex(static x => x.Key).IsUnique();

        _ = builder
            .Property(static x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(TemplateSnippet.DescriptionMaxLength);

        _ = builder
            .Property(static x => x.Documentation)
            .HasColumnName("documentation")
            .HasMaxLength(TemplateSnippet.DocumentationMaxLength);

        _ = builder
            .Property(static x => x.Markup)
            .HasColumnName("markup")
            .HasMaxLength(TemplateSnippet.MarkupMaxLength)
            .IsRequired();

        _ = builder
            .Property(static x => x.OutputMode)
            .HasColumnName("output_mode")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }

    private static void ConfigureJsonColumns(EntityTypeBuilder<TemplateSnippet> builder)
    {
        var paramsConverter = new ValueConverter<Dictionary<string, string>, string>(
            static v => JsonSerializer.Serialize(v, _jsonOptions),
            static v => DeserializeParams(v)
        );
        var paramsComparer = new ValueComparer<Dictionary<string, string>>(
            static (a, b) => DictionariesEqual(a, b),
            static v => ComputeDictionaryHash(v),
            static v => new Dictionary<string, string>(v, StringComparer.OrdinalIgnoreCase)
        );
        _ = builder
            .Property<Dictionary<string, string>>("_defaultParameters")
            .HasField("_defaultParameters")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("default_parameters")
            .HasColumnType("jsonb")
            .HasConversion(paramsConverter, paramsComparer)
            .IsRequired();

        var categoriesConverter = new ValueConverter<List<Guid>, string>(
            static v => JsonSerializer.Serialize(v, _jsonOptions),
            static v => DeserializeGuids(v)
        );
        var categoriesComparer = new ValueComparer<List<Guid>>(
            static (a, b) => a != null && b != null && a.SequenceEqual(b),
            static v => ComputeGuidListHash(v),
            static v => v.ToList()
        );
        _ = builder
            .Property<List<Guid>>("_categoryIds")
            .HasField("_categoryIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("category_ids")
            .HasColumnType("jsonb")
            .HasConversion(categoriesConverter, categoriesComparer)
            .IsRequired();
    }

    private static void ConfigureAuditAndIgnoredColumns(EntityTypeBuilder<TemplateSnippet> builder)
    {
        _ = builder.Property(static x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(static x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder
            .Property(static x => x.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();
        _ = builder.Property(static x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(static x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.Ignore(static x => x.DefaultParameters);
        _ = builder.Ignore(static x => x.CategoryIds);
        _ = builder.Ignore(static x => x.EntityTypeKey);
        _ = builder.Ignore(static x => x.EntityId);
        _ = builder.Ignore(static x => x.ParentAuthority);
    }

    private static Dictionary<string, string> DeserializeParams(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
        return parsed is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    private static List<Guid> DeserializeGuids(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<Guid>>(json, _jsonOptions) ?? [];
    }

    private static bool DictionariesEqual(
        Dictionary<string, string>? left,
        Dictionary<string, string>? right
    )
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (
                !right.TryGetValue(pair.Key, out var other)
                || !string.Equals(pair.Value, other, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    private static int ComputeDictionaryHash(Dictionary<string, string> dict)
    {
        var hash = new HashCode();
        foreach (var pair in dict.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static int ComputeGuidListHash(List<Guid> list)
    {
        var hash = new HashCode();
        foreach (var id in list)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
