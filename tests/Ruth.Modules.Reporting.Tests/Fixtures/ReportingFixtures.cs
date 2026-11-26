using Xunit;

namespace Ruth.Modules.Reporting.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class ReportingFixtures : ICollectionFixture<ReportingContainersFixture>
{
    public const string Name = "ReportingContainers";
}
