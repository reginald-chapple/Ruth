using Xunit;

namespace Ruth.Persistence.Tests.Fixtures;

[CollectionDefinition("Postgres18")]
public sealed class Postgres18FixtureCollectionDefinition
    : ICollectionFixture<Postgres18ContainerFixture>;
