using Xunit;

namespace Ruth.Modules.Communication.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CommunicationPostgresFixtures
    : ICollectionFixture<CommunicationPostgresContainerFixture>
{
    public const string Name = "CommunicationPostgres";
}
