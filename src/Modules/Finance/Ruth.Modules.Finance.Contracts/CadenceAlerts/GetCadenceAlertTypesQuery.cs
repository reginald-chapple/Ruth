using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Query to list configured transaction cadence and spike alert rules.</summary>
/// <param name="ActiveOnly">When true, returns only active rules.</param>
public sealed record GetCadenceAlertTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<TransactionCadenceAlertTypeResponse>>;
