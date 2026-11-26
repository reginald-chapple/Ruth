using Xunit;

namespace Ruth.Modules.Search.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class SearchFixtures : ICollectionFixture<SearchContainersFixture>
{
    public const string Name = "SearchContainers";
}
