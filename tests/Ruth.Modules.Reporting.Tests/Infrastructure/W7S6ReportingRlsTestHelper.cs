using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Infrastructure;

internal static class W7S6ReportingRlsTestHelper
{
    public static void VerifyTenantScopedAstCompilation(TenantId tenantA)
    {
        var emptyNode = new DataViewFilterNodeDto(DataViewFilterExpressionType.GroupAll);
        var cols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var unsetCompiled = DataViewSqlCompiler.CompileForTenant(TenantId.Empty, emptyNode, cols);
        Assert.Equal("FALSE", unsetCompiled.WhereClause);

        var tenantCompiled = DataViewSqlCompiler.CompileForTenant(tenantA, emptyNode, cols);
        Assert.Contains(
            "tenant_id = @tenantId",
            tenantCompiled.WhereClause,
            StringComparison.Ordinal
        );
        Assert.Equal(tenantA.Value, tenantCompiled.Parameters["@tenantId"]);
    }

    public static async Task VerifyBiViewsEnforceAppRoleRlsAndGucPredicateAsync(
        string adminConn,
        ServiceProvider provider,
        TenantId tenantA,
        TenantId tenantB
    )
    {
        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();

        await using (var unsetCmd = appConn.CreateCommand())
        {
            unsetCmd.CommandText = "SELECT COUNT(*) FROM reporting.vw_bi_metric_facts";
            Assert.Equal(
                0L,
                Convert.ToInt64(await unsetCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
            );
        }

        await AssertBiViewSumForTenantAsync(appConn, tenantA, expectedSum: 10m);
        await AssertBiViewSumForTenantAsync(appConn, tenantB, expectedSum: 25m);
    }

    public static async Task AssertAllReportingTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'reporting' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(
                    reader.GetBoolean(1),
                    $"Missing RLS on reporting.{reader.GetString(0)}"
                );
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on reporting.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 17,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 17 tables in reporting schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM reporting.data_views";
        Assert.Equal(
            0L,
            Convert.ToInt64(await appCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
        );
    }

    private static async Task AssertBiViewSumForTenantAsync(
        NpgsqlConnection appConn,
        TenantId tenantId,
        decimal expectedSum
    )
    {
        await using var tx = await appConn.BeginTransactionAsync();
        await using (var setCmd = appConn.CreateCommand())
        {
            setCmd.Transaction = tx;
            setCmd.CommandText = "SELECT set_config('app.current_tenant_id', @tid, true)";
            _ = setCmd.Parameters.AddWithValue("tid", tenantId.ToString());
            _ = await setCmd.ExecuteScalarAsync();
        }

        await using (var queryCmd = appConn.CreateCommand())
        {
            queryCmd.Transaction = tx;
            queryCmd.CommandText =
                "SELECT COALESCE(SUM(measure_sum), 0)::numeric FROM reporting.vw_bi_metric_facts";
            var sum = (decimal)(await queryCmd.ExecuteScalarAsync())!;
            Assert.Equal(expectedSum, sum);
        }

        await tx.CommitAsync();
    }
}
