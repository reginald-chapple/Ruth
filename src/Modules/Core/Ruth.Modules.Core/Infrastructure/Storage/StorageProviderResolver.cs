using System.Collections.Frozen;
using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Modules.Core.Infrastructure.Storage;

internal sealed class StorageProviderResolver : IStorageProviderResolver
{
    private readonly FrozenDictionary<string, IStorageProvider> _byKey;
    private readonly IStorageProvider? _fallback;

    public StorageProviderResolver(IEnumerable<IStorageProvider> providers)
    {
        var map = new Dictionary<string, IStorageProvider>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            map[provider.ProviderKey] = provider;
        }

        _byKey = map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _fallback = map.TryGetValue("rustfs", out var rustFs)
            ? rustFs
            : map.Values.FirstOrDefault();
    }

    public IStorageProvider? Resolve(string providerKey)
    {
        return
            !string.IsNullOrWhiteSpace(providerKey)
            && _byKey.TryGetValue(providerKey.Trim(), out var exact)
            ? exact
            : _fallback;
    }
}
