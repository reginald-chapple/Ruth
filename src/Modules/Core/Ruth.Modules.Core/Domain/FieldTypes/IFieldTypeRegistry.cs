namespace Ruth.Modules.Core.Domain.FieldTypes;

/// <summary>Lock-free registry resolving modular <see cref="IFieldTypePlugin" /> instances by key.</summary>
public interface IFieldTypeRegistry
{
    /// <summary>Gets all registered field type plugins ordered by name.</summary>
    IReadOnlyList<IFieldTypePlugin> GetAll();

    /// <summary>Attempts to resolve a field type plugin by key (case-insensitive).</summary>
    bool TryGet(string key, out IFieldTypePlugin plugin);
}
