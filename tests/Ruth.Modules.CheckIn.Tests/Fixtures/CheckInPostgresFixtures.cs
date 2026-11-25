using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CheckInPostgresFixtures : ICollectionFixture<CheckInPostgresContainerFixture>
{
    public const string Name = "CheckInPostgres";
}
