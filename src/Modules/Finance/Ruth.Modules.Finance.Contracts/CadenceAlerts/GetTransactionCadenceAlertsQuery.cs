using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Query to list detected transaction cadence and spike alerts.</summary>
/// <param name="Status">Optional filter by alert status.</param>
/// <param name="AlertKind">Optional filter by alert kind.</param>
/// <param name="PersonId">Optional filter by constituent Person identifier.</param>
public sealed record GetTransactionCadenceAlertsQuery(
    CadenceAlertStatus? Status = null,
    CadenceAlertKind? AlertKind = null,
    Guid? PersonId = null
) : IQuery<IReadOnlyList<TransactionCadenceAlertResponse>>;
