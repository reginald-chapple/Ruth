using Xunit;

namespace Ruth.Modules.Lms.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class LmsFixtures : ICollectionFixture<LmsContainersFixture>
{
    public const string Name = "LmsContainers";
}
