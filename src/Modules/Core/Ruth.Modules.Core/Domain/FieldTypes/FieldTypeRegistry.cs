using System.Collections.Frozen;

namespace Ruth.Modules.Core.Domain.FieldTypes;

internal sealed class FieldTypeRegistry : IFieldTypeRegistry
{
    private readonly FrozenDictionary<string, IFieldTypePlugin> _byKey;
    private readonly IReadOnlyList<IFieldTypePlugin> _ordered;

    public FieldTypeRegistry(IEnumerable<IFieldTypePlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var map = new Dictionary<string, IFieldTypePlugin>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in plugins)
        {
            map[plugin.Key.Trim()] = plugin;
        }

        _byKey = map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _ordered = [.. map.Values.OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public IReadOnlyList<IFieldTypePlugin> GetAll()
    {
        return _ordered;
    }

    public bool TryGet(string key, out IFieldTypePlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            plugin = null!;
            return false;
        }

        return _byKey.TryGetValue(key.Trim(), out plugin!);
    }
}
