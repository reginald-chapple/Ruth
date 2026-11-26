using Xunit;

namespace Ruth.Modules.Engagement.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class EngagementPostgresFixtures
    : ICollectionFixture<EngagementPostgresContainerFixture>
{
    public const string Name = "EngagementPostgres18";
}
