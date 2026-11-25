using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Cached Double Metaphone phonetic index entry keyed by normalized uppercase name token.</summary>
public sealed class MetaphoneEntry : IAuditableEntity
{
    private MetaphoneEntry() { }

    private MetaphoneEntry(string name, string primaryKey, string? alternateKey)
    {
        Name = name;
        PrimaryKey = primaryKey;
        AlternateKey = alternateKey;
    }

    /// <summary>Gets the uppercase name token (primary key).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the primary 4-character Double Metaphone key.</summary>
    public string PrimaryKey { get; private set; } = string.Empty;

    /// <summary>Gets the optional alternate 4-character Double Metaphone key.</summary>
    public string? AlternateKey { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <summary>Creates a metaphone index entry if <paramref name="rawName" /> encodes to a non-empty key.</summary>
    public static MetaphoneEntry? TryCreate(string? rawName)
    {
        var sanitized = CrmTextPolicy.SanitizePersonName(rawName);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        var upper =
            sanitized.Length > CrmTextPolicy.NamePartMaxLength
                ? sanitized[..CrmTextPolicy.NamePartMaxLength].ToUpperInvariant()
                : sanitized.ToUpperInvariant();
        var encoded = DoubleMetaphoneEncoder.Encode(upper);
        return string.IsNullOrWhiteSpace(encoded.PrimaryKey)
            ? null
            : new MetaphoneEntry(upper, encoded.PrimaryKey, encoded.AlternateKey);
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
