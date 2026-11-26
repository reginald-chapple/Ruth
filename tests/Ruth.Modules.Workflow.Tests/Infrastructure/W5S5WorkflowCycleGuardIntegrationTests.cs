using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

[Collection(WorkflowPostgresFixtures.Name)]
public sealed class W5S5WorkflowCycleGuardIntegrationTests(
    WorkflowPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task Engine_CyclicActivityLoop_StopsAtStepBudgetAndFaultsDeterministically()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await WorkflowTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var cyclicType = await commands.SendAsync<CreateWorkflowTypeCommand, WorkflowTypeResponse>(
            new CreateWorkflowTypeCommand(
                new WorkflowTypeSpec(
                    "Cyclic Guard Test",
                    "cyclic-guard-test",
                    "Loop",
                    "CYC-",
                    [
                        new WorkflowActivityTypeSpec(
                            "ping",
                            "Ping",
                            Order: 0,
                            IsActivatedWithWorkflow: true,
                            Actions:
                            [
                                new WorkflowActionTypeSpec(
                                    "go-pong",
                                    "Activate Pong",
                                    "workflow.control.activate-activity",
                                    Order: 0,
                                    IsActivityCompletedOnSuccess: true,
                                    Configuration: WorkflowEngineTestSeedHelper.Map(
                                        ("TargetActivityKey", "pong")
                                    )
                                ),
                            ]
                        ),
                        new WorkflowActivityTypeSpec(
                            "pong",
                            "Pong",
                            Order: 1,
                            Actions:
                            [
                                new WorkflowActionTypeSpec(
                                    "go-ping",
                                    "Activate Ping",
                                    "workflow.control.activate-activity",
                                    Order: 0,
                                    IsActivityCompletedOnSuccess: true,
                                    Configuration: WorkflowEngineTestSeedHelper.Map(
                                        ("TargetActivityKey", "ping")
                                    )
                                ),
                            ]
                        ),
                    ],
                    MaxProcessingAttempts: 1
                )
            )
        );
        Assert.True(cyclicType.IsSuccess);

        var launched = await commands.SendAsync<LaunchWorkflowCommand, WorkflowInstanceResponse>(
            new LaunchWorkflowCommand(cyclicType.Value.Id.ToString("D"), ProcessImmediately: true)
        );
        Assert.True(launched.IsSuccess);
        Assert.Equal(WorkflowInstanceStatus.Faulted, launched.Value.Status);
        Assert.Contains(launched.Value.Logs, static l =>
            l.EntryKind == WorkflowLogKind.Error
            && l.Summary.Contains("step limit", StringComparison.OrdinalIgnoreCase)
        );
    }
}
