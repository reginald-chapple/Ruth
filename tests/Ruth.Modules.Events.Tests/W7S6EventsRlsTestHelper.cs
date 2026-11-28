using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Events.Tests;

internal static class W7S6EventsRlsTestHelper
{
    public static void VerifySignalRGroupNamesRequireAndPartitionByTenant(
        TenantId tenantA,
        TenantId tenantB,
        Guid sharedId
    )
    {
        var groupA = InteractiveSessionHub.GetSessionGroupName(tenantA, sharedId);
        var groupB = InteractiveSessionHub.GetSessionGroupName(tenantB, sharedId);
        Assert.StartsWith($"t:{tenantA}:", groupA, StringComparison.Ordinal);
        Assert.StartsWith($"t:{tenantB}:", groupB, StringComparison.Ordinal);
        Assert.NotEqual(groupA, groupB, StringComparer.Ordinal);

        var codeA = InteractiveSessionHub.GetJoinCodeGroupName(tenantA, "ABC123");
        var codeB = InteractiveSessionHub.GetJoinCodeGroupName(tenantB, "ABC123");
        Assert.NotEqual(codeA, codeB, StringComparer.Ordinal);

        _ = Assert.Throws<TenantContextRequiredException>(() =>
            InteractiveSessionHub.GetSessionGroupName(TenantId.Empty, sharedId)
        );
        _ = Assert.Throws<TenantContextRequiredException>(() =>
            InteractiveSessionHub.GetJoinCodeGroupName(TenantId.Empty, "ABC123")
        );
    }

    public static async Task AssertAllEventsTablesHaveForceRlsAndFailClosedAsync(
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
                + "WHERE n.nspname = 'events' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
            await using var reader = await cmd.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.True(reader.GetBoolean(1), $"Missing RLS on events.{reader.GetString(0)}");
                Assert.True(
                    reader.GetBoolean(2),
                    $"Missing FORCE RLS on events.{reader.GetString(0)}"
                );
            }

            Assert.True(
                count >= 12,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected >= 12 tables in events schema, saw {count}."
                )
            );
        }

        var provisioner = provider.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConn)
        );
        await appConn.OpenAsync();
        await using var appCmd = appConn.CreateCommand();
        appCmd.CommandText = "SELECT COUNT(*) FROM events.event_calendars";
        var unsetCount = Convert.ToInt64(
            await appCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        Assert.Equal(0L, unsetCount);
    }
}
