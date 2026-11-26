using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Command to update an existing transaction cadence or spike alert rule.</summary>
/// <param name="AlertTypeId">Target alert type identifier.</param>
/// <param name="Spec">Updated rule specification.</param>
public sealed record UpdateCadenceAlertTypeCommand(
    Guid AlertTypeId,
    TransactionCadenceAlertTypeSpec Spec
) : ICommand<TransactionCadenceAlertTypeResponse>;
