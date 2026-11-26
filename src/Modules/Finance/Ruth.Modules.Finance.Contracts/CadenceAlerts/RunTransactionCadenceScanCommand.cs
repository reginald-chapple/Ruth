using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Command to evaluate constituent transaction histories and recurring schedules for lapse risk and spikes.</summary>
/// <param name="AsOfDate">Optional reference evaluation date (defaults to UTC today).</param>
public sealed record RunTransactionCadenceScanCommand(DateOnly? AsOfDate = null)
    : ICommand<CadenceScanSummaryResponse>;
