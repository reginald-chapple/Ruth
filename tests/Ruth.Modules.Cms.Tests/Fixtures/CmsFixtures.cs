using Xunit;

namespace Ruth.Modules.Cms.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CmsFixtures : ICollectionFixture<CmsContainersFixture>
{
    public const string Name = "CmsPostgresAndRedis";
}
