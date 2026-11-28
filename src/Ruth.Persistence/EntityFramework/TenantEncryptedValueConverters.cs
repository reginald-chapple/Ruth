using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// EF Core value-converter helpers enforcing per-tenant AES-256-GCM envelope encryption at rest
/// with <see cref="TenantId" /> and field/key purpose bound into associated authenticated data.
/// </summary>
public static class TenantEncryptedValueConverters
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Configures an optional secret string column to be stored exclusively as a per-tenant AES-256-GCM envelope.</summary>
    public static PropertyBuilder<string?> ConfigureEncryptedSecretColumn<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, string?>> propertyExpression,
        string columnName,
        string purpose,
        int maxLength = 2048
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var converter = new ValueConverter<string?, string?>(
            plaintext => ProtectOptionalString(plaintext, purpose),
            ciphertext => UnprotectOptionalString(ciphertext, purpose)
        );

        return builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasMaxLength(maxLength)
            .HasConversion(converter);
    }

    /// <summary>
    /// Maps a string dictionary to a PostgreSQL JSONB column while envelope-encrypting sensitive credential entries
    /// (or all entries when <paramref name="encryptAllValues" /> is true) with per-tenant + per-key AAD.
    /// </summary>
    public static void ConfigureEncryptedJsonbStringMap<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyDictionary<string, string>>> propertyExpression,
        string columnName,
        string purpose,
        bool encryptAllValues = false
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var converter = new ValueConverter<IReadOnlyDictionary<string, string>, string>(
            dict => SerializeEncryptedStringMap(dict, purpose, encryptAllValues),
            json => DeserializeEncryptedStringMap(json, purpose, encryptAllValues)
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(EncryptedMapValueComparers.StringMapComparer);
    }

    /// <summary>
    /// Maps a nullable-value string dictionary to a PostgreSQL JSONB column while envelope-encrypting sensitive credential entries.
    /// </summary>
    public static void ConfigureEncryptedJsonbNullableMap<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IReadOnlyDictionary<string, string?>>> propertyExpression,
        string columnName,
        string purpose,
        bool encryptAllValues = false
    )
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var converter = new ValueConverter<IReadOnlyDictionary<string, string?>, string>(
            dict => SerializeEncryptedNullableMap(dict, purpose, encryptAllValues),
            json => DeserializeEncryptedNullableMap(json, purpose, encryptAllValues)
        );

        var property = builder
            .Property(propertyExpression)
            .HasColumnName(columnName)
            .HasColumnType("jsonb")
            .HasConversion(converter)
            .IsRequired();
        property.Metadata.SetValueComparer(EncryptedMapValueComparers.NullableMapComparer);
    }

    private static string? ProtectOptionalString(string? value, string purpose)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var protector = TenantCredentialEncryptionContext.Current;
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (protector.IsProtectedEnvelope(value))
        {
            _ = protector.Unprotect(tenantId, purpose, value);
            return value;
        }

        return protector.Protect(tenantId, purpose, value);
    }

    private static string? UnprotectOptionalString(string? dbValue, string purpose)
    {
        return string.IsNullOrEmpty(dbValue)
            ? dbValue
            : TenantCredentialEncryptionContext.Current.Unprotect(
                AmbientTenantContext.CurrentTenantId,
                purpose,
                dbValue
            );
    }

    private static string SerializeEncryptedStringMap(
        IReadOnlyDictionary<string, string> dict,
        string purpose,
        bool encryptAllValues
    )
    {
        var output = new Dictionary<string, string>(dict.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in dict)
        {
            var shouldEncrypt =
                !string.IsNullOrEmpty(pair.Value)
                && (encryptAllValues || SensitiveSettingRedactor.IsSensitiveKey(pair.Key));
            output[pair.Key] = shouldEncrypt
                ? ProtectOptionalString(pair.Value, BuildEntryPurpose(purpose, pair.Key))!
                : pair.Value;
        }

        return JsonSerializer.Serialize(output, _jsonOptions);
    }

    private static Dictionary<string, string> DeserializeEncryptedStringMap(
        string json,
        string purpose,
        bool encryptAllValues
    )
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
        if (parsed is null || parsed.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var protector = TenantCredentialEncryptionContext.Current;
        var result = new Dictionary<string, string>(parsed.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in parsed)
        {
            var mustDecrypt =
                !string.IsNullOrEmpty(pair.Value)
                && (
                    encryptAllValues
                    || SensitiveSettingRedactor.IsSensitiveKey(pair.Key)
                    || protector.IsProtectedEnvelope(pair.Value)
                );
            result[pair.Key] = mustDecrypt
                ? UnprotectOptionalString(pair.Value, BuildEntryPurpose(purpose, pair.Key))!
                : pair.Value;
        }

        return result;
    }

    private static string SerializeEncryptedNullableMap(
        IReadOnlyDictionary<string, string?> dict,
        string purpose,
        bool encryptAllValues
    )
    {
        var output = new Dictionary<string, string?>(dict.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in dict)
        {
            var shouldEncrypt =
                !string.IsNullOrEmpty(pair.Value)
                && (encryptAllValues || SensitiveSettingRedactor.IsSensitiveKey(pair.Key));
            output[pair.Key] = shouldEncrypt
                ? ProtectOptionalString(pair.Value, BuildEntryPurpose(purpose, pair.Key))
                : pair.Value;
        }

        return JsonSerializer.Serialize(output, _jsonOptions);
    }

    private static Dictionary<string, string?> DeserializeEncryptedNullableMap(
        string json,
        string purpose,
        bool encryptAllValues
    )
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json, _jsonOptions);
        if (parsed is null || parsed.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var protector = TenantCredentialEncryptionContext.Current;
        var result = new Dictionary<string, string?>(
            parsed.Count,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var pair in parsed)
        {
            var mustDecrypt =
                !string.IsNullOrEmpty(pair.Value)
                && (
                    encryptAllValues
                    || SensitiveSettingRedactor.IsSensitiveKey(pair.Key)
                    || protector.IsProtectedEnvelope(pair.Value)
                );
            result[pair.Key] = mustDecrypt
                ? UnprotectOptionalString(pair.Value, BuildEntryPurpose(purpose, pair.Key))
                : pair.Value;
        }

        return result;
    }

    private static string BuildEntryPurpose(string basePurpose, string key)
    {
        return $"{basePurpose}:{key.Trim().ToLowerInvariant()}";
    }
}
