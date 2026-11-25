using System.Collections.Frozen;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;

namespace Ruth.Modules.Crm.Infrastructure.BackgroundChecks;

internal sealed class BackgroundCheckProviderResolver(
    IEnumerable<IBackgroundCheckProvider> providers
) : IBackgroundCheckProviderResolver
{
    private readonly FrozenDictionary<string, IBackgroundCheckProvider> _byKey = providers
        .GroupBy(static p => p.ProviderKey, StringComparer.OrdinalIgnoreCase)
        .ToFrozenDictionary(
            static g => g.Key,
            static g => g.Last(),
            StringComparer.OrdinalIgnoreCase
        );

    public IReadOnlyList<IBackgroundCheckProvider> GetAll()
    {
        return [.. _byKey.Values];
    }

    public IBackgroundCheckProvider? Resolve(string providerKey)
    {
        return string.IsNullOrWhiteSpace(providerKey) ? null
            : _byKey.TryGetValue(providerKey.Trim(), out var provider) ? provider
            : null;
    }
}
