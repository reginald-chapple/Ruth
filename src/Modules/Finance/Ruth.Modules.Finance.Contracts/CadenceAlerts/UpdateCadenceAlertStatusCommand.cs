using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Command to acknowledge, resolve, or dismiss a transaction cadence alert.</summary>
/// <param name="AlertId">Target cadence alert identifier.</param>
/// <param name="NewStatus">Target review status.</param>
/// <param name="Notes">Optional staff follow-up or resolution notes.</param>
public sealed record UpdateCadenceAlertStatusCommand(
    Guid AlertId,
    CadenceAlertStatus NewStatus,
    string? Notes = null
) : ICommand<TransactionCadenceAlertResponse>;
