using Xunit;

namespace Ruth.Modules.Crm.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class CrmPostgresFixtures : ICollectionFixture<CrmPostgresContainerFixture>
{
    public const string Name = "CrmPostgresCollection";
}
