using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.Modules.Lms.Tests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Lms.Tests.Infrastructure;

[Collection(LmsFixtures.Name)]
public sealed class W7S6LmsMultiTenantRlsIntegrationTests(LmsContainersFixture containers)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_CollidingCourseCodes_AndEnrollmentQueues_IsolateTenants()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var provider = await LmsTestHostFactory.CreateAsync(dbConn);

        await AssertAllLmsTablesHaveForceRlsAndFailClosedAsync(dbConn, provider);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var learnerA = Guid.CreateVersion7();
        var learnerB = Guid.CreateVersion7();

        var seededA = await SeedCurriculumAndEnrollAsync(provider, tenantA, "alpha-lms", learnerA);
        var seededB = await SeedCurriculumAndEnrollAsync(provider, tenantB, "beta-lms", learnerB);

        await VerifyCrossTenantLmsIsolationAsync(
            provider,
            tenantA,
            seededA,
            learnerA,
            tenantB,
            seededB,
            learnerB
        );
    }

    private static async Task<LmsCurriculumSeedHelper.SeededCurriculum> SeedCurriculumAndEnrollAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        Guid learnerId
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var seeded = await LmsCurriculumSeedHelper.SeedAsync(commands);
            var enrolled = await commands.SendAsync<
                EnrollLearningParticipantCommand,
                LearningParticipantResponse
            >(new EnrollLearningParticipantCommand(seeded.Class1Id, learnerId));
            Assert.True(enrolled.IsSuccess);
            return seeded;
        }
    }

    private static async Task VerifyCrossTenantLmsIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        LmsCurriculumSeedHelper.SeededCurriculum seededA,
        Guid learnerA,
        TenantId tenantB,
        LmsCurriculumSeedHelper.SeededCurriculum seededB,
        Guid learnerB
    )
    {
        Assert.NotEqual(seededA.Course1Id, seededB.Course1Id);
        Assert.NotEqual(seededA.Class1Id, seededB.Class1Id);

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-lms", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var coursesA = (
                await queries.QueryAsync<
                    GetLearningCoursesQuery,
                    IReadOnlyList<LearningCourseResponse>
                >(new GetLearningCoursesQuery())
            ).Value;
            Assert.Equal(2, coursesA.Count);
            Assert.DoesNotContain(coursesA, c => c.Id == seededB.Course1Id);

            var crossEnroll = await commands.SendAsync<
                EnrollLearningParticipantCommand,
                LearningParticipantResponse
            >(new EnrollLearningParticipantCommand(seededB.Class1Id, learnerA));
            Assert.True(crossEnroll.IsFailure);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-lms", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var portalAInB = (
                await queries.QueryAsync<
                    GetLearnerPortalQuery,
                    IReadOnlyList<LearnerClassWorkspaceResponse>
                >(new GetLearnerPortalQuery(learnerA))
            ).Value;
            Assert.Empty(portalAInB);

            var portalB = (
                await queries.QueryAsync<
                    GetLearnerPortalQuery,
                    IReadOnlyList<LearnerClassWorkspaceResponse>
                >(new GetLearnerPortalQuery(learnerB))
            ).Value;
            _ = Assert.Single(portalB);
        }
    }

    private static async Task AssertAllLmsTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'lms' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on lms.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on lms.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 12,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 12 tables in lms schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM lms.learning_courses";
        Assert.Equal(
            0L,
            Convert.ToInt64(await appCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
        );
    }
}
