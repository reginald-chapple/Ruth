using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Adjusts credits on an issued punch pass (positive credit or negative debit) and syncs Smart Wallet Passes.</summary>
/// <param name="IssuedPunchPassId">Target issued punch pass identifier.</param>
/// <param name="DeltaCredits">Non-zero signed credit adjustment.</param>
/// <param name="Reason">Required audit reason.</param>
public sealed record AdjustPunchPassBalanceCommand(
    Guid IssuedPunchPassId,
    int DeltaCredits,
    string Reason
) : ICommand<IssuedPunchPassResponse>;
