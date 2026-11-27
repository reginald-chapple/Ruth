using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Synchronizes the PostgreSQL app.current_tenant_id session GUC with <see cref="AmbientTenantContext.CurrentTenantId" />
/// across pooled ADO.NET connections, EF Core commands, raw SQL operations, and rolled-back transactions.
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

        public bool SetInsideTransaction { get; set; }

        public WeakReference<DbTransaction>? BoundTransaction { get; set; }

        public void Reset()
        {
            Value = null;
            SetInsideTransaction = false;
            BoundTransaction = null;
        }
    }

    /// <summary>Clears the cached GUC state for <paramref name="connection" /> (e.g. on open, close, or transaction rollback).</summary>
    public static void Invalidate(DbConnection? connection)
    {
        if (connection is not null && _boundTenants.TryGetValue(connection, out var holder))
        {
            holder.Reset();
        }
    }

    /// <summary>Synchronously ensures app.current_tenant_id matches <see cref="AmbientTenantContext.CurrentTenantId" />.</summary>
    public static void EnsureCurrentTenant(DbConnection? connection, DbTransaction? transaction)
    {
        if (connection is null || connection.State != ConnectionState.Open)
        {
            return;
        }

        var holder = GetOrRegisterHolder(connection);
        var desired = ResolveDesiredGucValue();
        if (IsAlreadyBound(holder, transaction, desired))
        {
            return;
        }

        using var command = CreateSetConfigCommand(connection, transaction, desired);
        _ = command.ExecuteScalar();
        RecordBound(holder, transaction, desired);
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

        var holder = GetOrRegisterHolder(connection);
        var desired = ResolveDesiredGucValue();
        if (IsAlreadyBound(holder, transaction, desired))
        {
            return;
        }

        await using var command = CreateSetConfigCommand(connection, transaction, desired);
        _ = await command.ExecuteScalarAsync(cancellationToken);
        RecordBound(holder, transaction, desired);
    }

    private static BoundTenantHolder GetOrRegisterHolder(DbConnection connection)
    {
        return _boundTenants.GetValue(connection, static conn =>
        {
            var created = new BoundTenantHolder();
            conn.StateChange += (_, e) =>
            {
                if (
                    e.CurrentState
                    is ConnectionState.Closed
                        or ConnectionState.Broken
                        or ConnectionState.Open
                )
                {
                    created.Reset();
                }
            };
            return created;
        });
    }

    private static string ResolveDesiredGucValue()
    {
        var tenantId = AmbientTenantContext.CurrentTenantId;
        return tenantId.IsEmpty ? string.Empty : tenantId.ToString();
    }

    private static bool IsAlreadyBound(
        BoundTenantHolder holder,
        DbTransaction? transaction,
        string desired
    )
    {
        if (!string.Equals(holder.Value, desired, StringComparison.Ordinal))
        {
            return false;
        }

        if (!holder.SetInsideTransaction)
        {
            return true;
        }

        // If the GUC was set inside a transaction, PostgreSQL reverts set_config(..., false) when
        // that transaction rolls back (including uncommitted DisposeAsync). Only trust the cached
        // value while executing inside that exact same live DbTransaction instance.
        return transaction is not null
            && holder.BoundTransaction is not null
            && holder.BoundTransaction.TryGetTarget(out var boundTx)
            && ReferenceEquals(boundTx, transaction);
    }

    private static void RecordBound(
        BoundTenantHolder holder,
        DbTransaction? transaction,
        string desired
    )
    {
        holder.Value = desired;
        holder.SetInsideTransaction = transaction is not null;
        holder.BoundTransaction = transaction is not null
            ? new WeakReference<DbTransaction>(transaction)
            : null;
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
