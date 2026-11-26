using Xunit;

namespace Ruth.Modules.VirtualRooms.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class VirtualRoomsPostgresFixtures
    : ICollectionFixture<VirtualRoomsPostgresContainerFixture>
{
    public const string Name = "VirtualRoomsPostgres18";
}
