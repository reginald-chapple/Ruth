using Xunit;

namespace Ruth.Modules.Groups.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class GroupsPostgresFixtures : ICollectionFixture<GroupsPostgresContainerFixture>
{
    public const string Name = "GroupsPostgresCollection";
}
