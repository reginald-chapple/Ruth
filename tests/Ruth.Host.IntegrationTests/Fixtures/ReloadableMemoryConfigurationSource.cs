using Microsoft.Extensions.Configuration;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal sealed class ReloadableMemoryConfigurationSource(IDictionary<string, string?> initialData)
    : IConfigurationSource
{
    public ReloadableMemoryConfigurationProvider Provider { get; } = new(initialData);

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return Provider;
    }

    internal sealed class ReloadableMemoryConfigurationProvider : ConfigurationProvider
    {
        public ReloadableMemoryConfigurationProvider(IDictionary<string, string?> initialData)
        {
            foreach (var (key, value) in initialData)
            {
                Data[key] = value;
            }
        }

        public void SetAndReload(string key, string? value)
        {
            Data[key] = value;
            OnReload();
        }
    }
}
