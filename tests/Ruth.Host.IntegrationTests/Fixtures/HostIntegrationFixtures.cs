using Xunit;

namespace Ruth.Host.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class HostIntegrationFixtures : ICollectionFixture<HostContainersFixture>
{
    public const string Name = "HostIntegrationFixtures";
}
