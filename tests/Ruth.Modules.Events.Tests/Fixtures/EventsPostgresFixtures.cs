using Xunit;

namespace Ruth.Modules.Events.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class EventsPostgresFixtures : ICollectionFixture<EventsPostgresContainerFixture>
{
    public const string Name = "EventsPostgres";
}
