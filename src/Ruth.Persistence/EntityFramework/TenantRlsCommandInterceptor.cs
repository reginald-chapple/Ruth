using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Ensures app.current_tenant_id matches the ambient tenant before every EF Core relational command.
/// </summary>
public sealed class TenantRlsCommandInterceptor : DbCommandInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result
    )
    {
        SyncIfNeeded(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default
    )
    {
        await SyncIfNeededAsync(command, cancellationToken);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result
    )
    {
        SyncIfNeeded(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        await SyncIfNeededAsync(command, cancellationToken);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result
    )
    {
        SyncIfNeeded(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default
    )
    {
        await SyncIfNeededAsync(command, cancellationToken);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static void SyncIfNeeded(DbCommand? command)
    {
        if (IsSelfGucCommand(command))
        {
            return;
        }

        TenantRlsConnectionSynchronizer.EnsureCurrentTenant(
            command!.Connection,
            command.Transaction
        );
    }

    private static ValueTask SyncIfNeededAsync(
        DbCommand? command,
        CancellationToken cancellationToken
    )
    {
        return IsSelfGucCommand(command)
            ? ValueTask.CompletedTask
            : TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
                command!.Connection,
                command.Transaction,
                cancellationToken
            );
    }

    private static bool IsSelfGucCommand(DbCommand? command)
    {
        return command is null
            || string.Equals(
                command.CommandText,
                TenantRlsConnectionSynchronizer.SetTenantGucSql,
                StringComparison.Ordinal
            );
    }
}
