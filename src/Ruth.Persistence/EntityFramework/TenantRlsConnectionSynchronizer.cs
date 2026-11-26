using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Synchronizes the PostgreSQL app.current_tenant_id session GUC with <see cref="AmbientTenantContext.CurrentTenantId" />
/// across pooled ADO.NET connections, EF Core commands, and raw SQL operations.
/// </summary>
public static class TenantRlsConnectionSynchronizer
{
    internal const string SetTenantGucSql =
        "SELECT set_config('app.current_tenant_id', @tid, false);";

    private static readonly ConditionalWeakTable<DbConnection, BoundTenantHolder> _boundTenants =
    [];

    private sealed class BoundTenantHolder
    {
        public string? Value { get; set; }
    }

    /// <summary>Clears the cached GUC state for <paramref name="connection" /> (e.g. on open, close, or transaction rollback).</summary>
    public static void Invalidate(DbConnection? connection)
    {
        if (connection is not null)
        {
            _ = _boundTenants.Remove(connection);
        }
    }

    /// <summary>Synchronously ensures app.current_tenant_id matches <see cref="AmbientTenantContext.CurrentTenantId" />.</summary>
    public static void EnsureCurrentTenant(DbConnection? connection, DbTransaction? transaction)
    {
        if (connection is null || connection.State != ConnectionState.Open)
        {
            return;
        }

        var desired = ResolveDesiredGucValue();
        if (IsAlreadyBound(connection, desired))
        {
            return;
        }

        using var command = CreateSetConfigCommand(connection, transaction, desired);
        _ = command.ExecuteScalar();
        RecordBound(connection, desired);
    }

    /// <summary>Asynchronously ensures app.current_tenant_id matches <see cref="AmbientTenantContext.CurrentTenantId" />.</summary>
    public static async ValueTask EnsureCurrentTenantAsync(
        DbConnection? connection,
        DbTransaction? transaction,
        CancellationToken cancellationToken = default
    )
    {
        if (connection is null || connection.State != ConnectionState.Open)
        {
            return;
        }

        var desired = ResolveDesiredGucValue();
        if (IsAlreadyBound(connection, desired))
        {
            return;
        }

        await using var command = CreateSetConfigCommand(connection, transaction, desired);
        _ = await command.ExecuteScalarAsync(cancellationToken);
        RecordBound(connection, desired);
    }

    private static string ResolveDesiredGucValue()
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty ? string.Empty : tenantId.ToString();
    }

    private static bool IsAlreadyBound(DbConnection connection, string desired)
    {
        return _boundTenants.TryGetValue(connection, out var holder)
            && string.Equals(holder.Value, desired, StringComparison.Ordinal);
    }

    private static void RecordBound(DbConnection connection, string desired)
    {
        var holder = _boundTenants.GetOrCreateValue(connection);
        holder.Value = desired;
    }

    private static DbCommand CreateSetConfigCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string desired
    )
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = SetTenantGucSql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tid";
        parameter.Value = desired;
        _ = command.Parameters.Add(parameter);
        return command;
    }
}
