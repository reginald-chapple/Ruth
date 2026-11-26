using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Synchronizes app.current_tenant_id before transactions start and invalidates cached GUC state on rollback
/// because PostgreSQL SET and set_config changes inside aborted transactions or rolled-back savepoints revert.
/// </summary>
public sealed class TenantRlsTransactionInterceptor : DbTransactionInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result
    )
    {
        TenantRlsConnectionSynchronizer.EnsureCurrentTenant(connection, transaction: null);
        return base.TransactionStarting(connection, eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default
    )
    {
        await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
            connection,
            transaction: null,
            cancellationToken
        );
        return await base.TransactionStartingAsync(
            connection,
            eventData,
            result,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override void TransactionRolledBack(
        DbTransaction transaction,
        TransactionEndEventData eventData
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        base.TransactionRolledBack(transaction!, eventData);
    }

    /// <inheritdoc />
    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        return base.TransactionRolledBackAsync(transaction!, eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override void RolledBackToSavepoint(
        DbTransaction transaction,
        TransactionEventData eventData
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        base.RolledBackToSavepoint(transaction!, eventData);
    }

    /// <inheritdoc />
    public override Task RolledBackToSavepointAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        return base.RolledBackToSavepointAsync(transaction!, eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override void TransactionFailed(
        DbTransaction transaction,
        TransactionErrorEventData eventData
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        base.TransactionFailed(transaction!, eventData);
    }

    /// <inheritdoc />
    public override Task TransactionFailedAsync(
        DbTransaction transaction,
        TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(transaction?.Connection);
        return base.TransactionFailedAsync(transaction!, eventData, cancellationToken);
    }
}
