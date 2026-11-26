using Xunit;

namespace Ruth.Modules.Commerce.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CommercePostgresFixtures : ICollectionFixture<CommercePostgresContainerFixture>
{
    public const string Name = "CommercePostgres";
}
