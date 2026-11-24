namespace Ruth.Host.Configuration;

internal static class ConfigurationValueResolver
{
    public static string? ResolveFirstNonEmpty(
        IConfiguration configuration,
        IReadOnlyList<string> priorityKeys
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(priorityKeys);

        if (configuration is IConfigurationRoot root)
        {
            var shadowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visitedRoots = new HashSet<IConfigurationRoot>(ReferenceEqualityComparer.Instance);
            return ResolveFromProviders(root, priorityKeys, shadowedKeys, visitedRoots);
        }

        return ResolveFromIndexer(configuration, priorityKeys);
    }

    private static string? ResolveFromProviders(
        IConfigurationRoot root,
        IReadOnlyList<string> priorityKeys,
        HashSet<string> shadowedKeys,
        HashSet<IConfigurationRoot> visitedRoots
    )
    {
        if (!visitedRoots.Add(root))
        {
            return null;
        }

        foreach (var provider in root.Providers.Reverse())
        {
            var found = TryResolveFromProvider(provider, priorityKeys, shadowedKeys, visitedRoots);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static string? TryResolveFromProvider(
        IConfigurationProvider provider,
        IReadOnlyList<string> priorityKeys,
        HashSet<string> shadowedKeys,
        HashSet<IConfigurationRoot> visitedRoots
    )
    {
        return provider switch
        {
            ChainedConfigurationProvider { Configuration: IConfigurationRoot chainedRoot } =>
                ResolveFromProviders(chainedRoot, priorityKeys, shadowedKeys, visitedRoots),
            ChainedConfigurationProvider chained => TryGetFromChainedConfiguration(
                chained.Configuration,
                priorityKeys,
                shadowedKeys
            ),
            _ => TryGetFromLeafProvider(provider, priorityKeys, shadowedKeys),
        };
    }

    private static string? TryGetFromLeafProvider(
        IConfigurationProvider provider,
        IReadOnlyList<string> priorityKeys,
        HashSet<string> shadowedKeys
    )
    {
        foreach (var key in priorityKeys)
        {
            if (shadowedKeys.Contains(key) || !provider.TryGet(key, out var value))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            _ = shadowedKeys.Add(key);
        }

        return null;
    }

    private static string? TryGetFromChainedConfiguration(
        IConfiguration configuration,
        IReadOnlyList<string> priorityKeys,
        HashSet<string> shadowedKeys
    )
    {
        foreach (var key in priorityKeys)
        {
            if (shadowedKeys.Contains(key))
            {
                continue;
            }

            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            if (value is not null || HasExplicitNullLeaf(configuration, key))
            {
                _ = shadowedKeys.Add(key);
            }
        }

        return null;
    }

    private static bool HasExplicitNullLeaf(IConfiguration configuration, string key)
    {
        var targetSection = configuration.GetSection(key);
        if (targetSection.GetChildren().Any())
        {
            return false;
        }

        var lastColon = key.LastIndexOf(':');
        var parent = lastColon > 0 ? configuration.GetSection(key[..lastColon]) : configuration;
        var leafName = lastColon > 0 ? key[(lastColon + 1)..] : key;

        return parent
            .GetChildren()
            .Any(child => string.Equals(child.Key, leafName, StringComparison.OrdinalIgnoreCase));
    }

    // Non-root IConfiguration (e.g., IConfigurationSection) does not publicly expose Providers,
    // so fallback resolution evaluates priorityKeys in key-major order via the indexer.
    private static string? ResolveFromIndexer(
        IConfiguration configuration,
        IReadOnlyList<string> priorityKeys
    )
    {
        foreach (var key in priorityKeys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
