using Xunit;

namespace Ruth.Modules.Workflow.Tests.Fixtures;

[CollectionDefinition(Name)]
public sealed class WorkflowPostgresFixtures : ICollectionFixture<WorkflowPostgresContainerFixture>
{
    public const string Name = "WorkflowPostgres18";
}
