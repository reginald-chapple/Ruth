using Xunit;

namespace Ruth.Modules.Finance.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class FinancePostgresFixtures : ICollectionFixture<FinancePostgresContainerFixture>
{
    public const string Name = "FinancePostgres";
}
