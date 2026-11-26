using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Command to cancel an unfulfilled financial pledge.</summary>
/// <param name="PledgeId">Target pledge identifier.</param>
/// <param name="Reason">Optional cancellation reason appended to notes.</param>
public sealed record CancelFinancialPledgeCommand(Guid PledgeId, string? Reason = null)
    : ICommand<FinancialPledgeResponse>;
