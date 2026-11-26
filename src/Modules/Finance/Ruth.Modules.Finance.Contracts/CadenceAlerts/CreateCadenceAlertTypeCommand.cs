using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Command to create a transaction cadence or spike alert rule.</summary>
/// <param name="Spec">Rule specification.</param>
public sealed record CreateCadenceAlertTypeCommand(TransactionCadenceAlertTypeSpec Spec)
    : ICommand<TransactionCadenceAlertTypeResponse>;
