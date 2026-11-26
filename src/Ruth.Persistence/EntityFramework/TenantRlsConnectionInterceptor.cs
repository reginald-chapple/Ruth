using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Resets and initializes the app.current_tenant_id PostgreSQL GUC whenever a pooled connection opens or closes.
/// </summary>
public sealed class TenantRlsConnectionInterceptor : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        TenantRlsConnectionSynchronizer.EnsureCurrentTenant(connection, transaction: null);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
            connection,
            transaction: null,
            cancellationToken
        );
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        return base.ConnectionClosingAsync(connection, eventData, result);
    }

    /// <inheritdoc />
    public override void ConnectionFailed(
        DbConnection connection,
        ConnectionErrorEventData eventData
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        base.ConnectionFailed(connection, eventData);
    }

    /// <inheritdoc />
    public override Task ConnectionFailedAsync(
        DbConnection connection,
        ConnectionErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        TenantRlsConnectionSynchronizer.Invalidate(connection);
        return base.ConnectionFailedAsync(connection, eventData, cancellationToken);
    }
}
