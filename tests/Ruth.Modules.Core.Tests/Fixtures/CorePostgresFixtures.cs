using Xunit;

namespace Ruth.Modules.Core.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CorePostgresFixtures : ICollectionFixture<CorePostgresContainerFixture>
{
    public const string Name = "CorePostgres18";
}
