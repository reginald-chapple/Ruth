using System.Collections.Frozen;
using Ruth.Modules.Communication.Contracts.Templates;

namespace Ruth.Modules.Communication.Infrastructure.Templating;

internal sealed class RazorTemplateRegistry : IRazorTemplateRegistry
{
    private readonly FrozenDictionary<string, RazorTemplateDescriptor> _byKey;
    private readonly RazorTemplateDescriptor[] _ordered;

    public RazorTemplateRegistry(IEnumerable<RazorTemplateDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var map = new Dictionary<string, RazorTemplateDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var descriptor in descriptors)
        {
            map[descriptor.Key] = descriptor;
        }

        _byKey = map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _ordered =
        [
            .. map
                .Values.OrderBy(static d => d.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static d => d.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    public IReadOnlyList<RazorTemplateDescriptor> GetAll()
    {
        return _ordered;
    }

    public RazorTemplateDescriptor? FindByKey(string templateKey)
    {
        return string.IsNullOrWhiteSpace(templateKey)
            ? null
            : _byKey.GetValueOrDefault(templateKey.Trim());
    }
}
