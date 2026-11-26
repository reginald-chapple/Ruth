using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class GetTransactionCadenceAlertsQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<
        GetTransactionCadenceAlertsQuery,
        IReadOnlyList<TransactionCadenceAlertResponse>
    >
{
    public async Task<Result<IReadOnlyList<TransactionCadenceAlertResponse>>> HandleAsync(
        GetTransactionCadenceAlertsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var alertsQuery = dbContext.CadenceAlerts.AsNoTracking().AsQueryable();
        if (query.Status is { } status)
        {
            alertsQuery = alertsQuery.Where(a => a.Status == status);
        }

        if (query.AlertKind is { } kind)
        {
            alertsQuery = alertsQuery.Where(a => a.AlertKind == kind);
        }

        if (query.PersonId is { } personId && personId != Guid.Empty)
        {
            alertsQuery = alertsQuery.Where(a => a.PersonId == personId);
        }

        var alerts = await alertsQuery
            .OrderByDescending(static a => a.DetectedOnUtc)
            .ToListAsync(cancellationToken);
        if (alerts.Count == 0)
        {
            IReadOnlyList<TransactionCadenceAlertResponse> empty = [];
            return Result.Success(empty);
        }

        var typeIds = alerts.Select(static a => a.AlertTypeId).Distinct().ToArray();
        var typeNames = await dbContext
            .CadenceAlertTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        IReadOnlyList<TransactionCadenceAlertResponse> mapped =
        [
            .. alerts.Select(a =>
                CadenceAlertMapper.ToAlertResponse(
                    a,
                    typeNames.TryGetValue(a.AlertTypeId, out var tName) ? tName : "Cadence Rule"
                )
            ),
        ];

        return Result.Success(mapped);
    }
}
