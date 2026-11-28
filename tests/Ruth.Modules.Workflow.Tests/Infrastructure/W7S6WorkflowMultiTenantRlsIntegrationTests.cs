using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Tests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

[Collection(WorkflowPostgresFixtures.Name)]
public sealed class W7S6WorkflowMultiTenantRlsIntegrationTests(
    WorkflowPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_CollidingWorkflowSlugs_AndInstanceExecution_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await WorkflowTestHostFactory.BuildAndMigrateAsync(dbConn);

        await AssertAllWorkflowTablesHaveForceRlsAndFailClosedAsync(dbConn, provider);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var (wfTypeA, instanceA) = await SeedAndLaunchWorkflowAsync(
            provider,
            tenantA,
            "alpha-wf",
            "Alpha Grant Review"
        );
        var (wfTypeB, instanceB) = await SeedAndLaunchWorkflowAsync(
            provider,
            tenantB,
            "beta-wf",
            "Beta Case Review"
        );

        await VerifyCrossTenantWorkflowIsolationAsync(
            provider,
            tenantA,
            wfTypeA,
            instanceA,
            tenantB,
            wfTypeB,
            instanceB
        );
    }

    private static async Task<(Guid WorkflowTypeId, Guid InstanceId)> SeedAndLaunchWorkflowAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string instanceName
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var spec = new WorkflowTypeSpec(
                "Grant Intake Workflow",
                "grant-intake-workflow",
                "Grants",
                "GRT-",
                [
                    new WorkflowActivityTypeSpec(
                        "triage",
                        "Initial Triage",
                        Order: 0,
                        IsActivatedWithWorkflow: true,
                        Actions:
                        [
                            new WorkflowActionTypeSpec(
                                "mark-triaged",
                                "Mark Triaged",
                                "workflow.attributes.set-value",
                                Order: 0,
                                Configuration: new Dictionary<string, string?>(
                                    StringComparer.OrdinalIgnoreCase
                                )
                                {
                                    ["AttributeKey"] = "TriageState",
                                    ["Value"] = "Ready",
                                }
                            ),
                        ]
                    ),
                ],
                Attributes:
                [
                    new WorkflowAttributeSpec("TriageState", "Triage State", "text", "New"),
                ]
            );

            var wfType = (
                await commands.SendAsync<CreateWorkflowTypeCommand, WorkflowTypeResponse>(
                    new CreateWorkflowTypeCommand(spec)
                )
            ).Value;

            var launched = (
                await commands.SendAsync<LaunchWorkflowCommand, WorkflowInstanceResponse>(
                    new LaunchWorkflowCommand(wfType.Slug, instanceName, ProcessImmediately: true)
                )
            ).Value;

            return (wfType.Id, launched.Id);
        }
    }

    private static async Task VerifyCrossTenantWorkflowIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid wfTypeA,
        Guid instanceA,
        TenantId tenantB,
        Guid wfTypeB,
        Guid instanceB
    )
    {
        Assert.NotEqual(wfTypeA, wfTypeB);
        Assert.NotEqual(instanceA, instanceB);

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-wf", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var typesA = (
                await queries.QueryAsync<
                    GetWorkflowTypesQuery,
                    IReadOnlyList<WorkflowTypeResponse>
                >(new GetWorkflowTypesQuery(IncludeInactive: true))
            ).Value;
            var singleA = Assert.Single(typesA);
            Assert.Equal(wfTypeA, singleA.Id);

            var crossRead = await queries.QueryAsync<
                GetWorkflowInstanceByIdQuery,
                WorkflowInstanceResponse
            >(new GetWorkflowInstanceByIdQuery(instanceB));
            Assert.True(crossRead.IsFailure);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-wf", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var typesB = (
                await queries.QueryAsync<
                    GetWorkflowTypesQuery,
                    IReadOnlyList<WorkflowTypeResponse>
                >(new GetWorkflowTypesQuery(IncludeInactive: true))
            ).Value;
            var singleB = Assert.Single(typesB);
            Assert.Equal(wfTypeB, singleB.Id);
        }
    }

    private static async Task AssertAllWorkflowTablesHaveForceRlsAndFailClosedAsync(
        string adminConn,
        ServiceProvider provider
    )
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
                + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
                + "WHERE n.nspname = 'workflow' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on workflow.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on workflow.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 12,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 12 tables in workflow schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM workflow.workflow_types";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
