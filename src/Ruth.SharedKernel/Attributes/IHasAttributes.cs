namespace Ruth.SharedKernel.Attributes;

/// <summary>Marks a domain entity that stores extensible typed attribute values in a PostgreSQL JSONB column.</summary>
public interface IHasAttributes
{
    /// <summary>Gets the current attribute key/value map.</summary>
    IReadOnlyDictionary<string, string?> AttributeValues { get; }

    /// <summary>Sets or updates an attribute value by key.</summary>
    void SetAttributeValue(string key, string? value);

    /// <summary>Removes an attribute value by key.</summary>
    void RemoveAttributeValue(string key);
}
