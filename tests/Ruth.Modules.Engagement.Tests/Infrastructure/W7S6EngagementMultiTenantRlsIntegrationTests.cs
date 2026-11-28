using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W7S6EngagementMultiTenantRlsIntegrationTests(
    EngagementPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_CollidingPipelineSlugs_AndPublicSignup_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(dbConn);

        await AssertAllEngagementTablesHaveForceRlsAndFailClosedAsync(dbConn, provider);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var pipelineA = await SeedPipelineAndSubmitSignupAsync(
            provider,
            tenantA,
            "alpha-engage",
            "Elena",
            "elena@alpha.org"
        );
        var pipelineB = await SeedPipelineAndSubmitSignupAsync(
            provider,
            tenantB,
            "beta-engage",
            "Marcus",
            "marcus@beta.org"
        );

        await VerifyCrossTenantIsolationAsync(provider, tenantA, pipelineA, tenantB, pipelineB);
    }

    private static async Task<Guid> SeedPipelineAndSubmitSignupAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string firstName,
        string email
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var pipe = (
                await commands.SendAsync<CreatePipelineTypeCommand, PipelineTypeResponse>(
                    new CreatePipelineTypeCommand(
                        new PipelineTypeSpec(
                            "Volunteer Onboarding",
                            "volunteer-onboarding",
                            [new PipelineStageSpec("New Intake", 0, IsInitial: true)]
                        )
                    )
                )
            ).Value;

            var opp = (
                await commands.SendAsync<
                    CreateEngagementOpportunityCommand,
                    EngagementOpportunityResponse
                >(
                    new CreateEngagementOpportunityCommand(
                        new EngagementOpportunitySpec(
                            pipe.Id,
                            "Community Pantry Team",
                            "community-pantry-team"
                        )
                    )
                )
            ).Value;

            var req = await commands.SendAsync<
                SubmitPublicEngagementSignupCommand,
                EngagementRequestResponse
            >(
                new SubmitPublicEngagementSignupCommand(
                    opp.Slug,
                    firstName,
                    "Volunteer",
                    email,
                    Comments: $"Intake for {slug}"
                )
            );
            Assert.True(req.IsSuccess);
            return pipe.Id;
        }
    }

    private static async Task VerifyCrossTenantIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid pipelineA,
        TenantId tenantB,
        Guid pipelineB
    )
    {
        Assert.NotEqual(pipelineA, pipelineB);

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-engage", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var pipesA = (
                await queries.QueryAsync<
                    GetPipelineTypesQuery,
                    IReadOnlyList<PipelineTypeResponse>
                >(new GetPipelineTypesQuery(ActiveOnly: false))
            ).Value;
            var singlePipeA = Assert.Single(pipesA);
            Assert.Equal(pipelineA, singlePipeA.Id);

            var boardA = (
                await queries.QueryAsync<GetConnectorBoardQuery, ConnectorBoardResponse>(
                    new GetConnectorBoardQuery(pipelineA, IncludeClosed: true)
                )
            ).Value;
            Assert.Equal(1, boardA.TotalActiveCount);

            var crossBoard = await queries.QueryAsync<
                GetConnectorBoardQuery,
                ConnectorBoardResponse
            >(new GetConnectorBoardQuery(pipelineB, IncludeClosed: true));
            Assert.True(crossBoard.IsFailure);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-engage", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var pipesB = (
                await queries.QueryAsync<
                    GetPipelineTypesQuery,
                    IReadOnlyList<PipelineTypeResponse>
                >(new GetPipelineTypesQuery(ActiveOnly: false))
            ).Value;
            var singlePipeB = Assert.Single(pipesB);
            Assert.Equal(pipelineB, singlePipeB.Id);
        }
    }

    private static async Task AssertAllEngagementTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'engage' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on engage.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on engage.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 16,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 16 tables in engage schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM engage.pipeline_types";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
