using Xunit;

namespace Ruth.Modules.Identity.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class IdentityPostgresFixtures : ICollectionFixture<Postgres18ContainerFixture>
{
    public const string Name = "IdentityPostgres";
}
