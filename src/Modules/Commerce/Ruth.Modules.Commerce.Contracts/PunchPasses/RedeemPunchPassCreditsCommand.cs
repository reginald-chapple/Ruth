using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Redeems one or more credits from an active punch pass and syncs Smart Wallet Passes.</summary>
/// <param name="IssuedPunchPassId">Target issued punch pass identifier.</param>
/// <param name="CreditsToRedeem">Positive number of credits to deduct.</param>
/// <param name="RedeemedForPersonId">Optional beneficiary person (defaults to owner; validated for household sharing if different).</param>
/// <param name="BranchId">Optional Branch/Site where redemption occurs.</param>
/// <param name="ReferenceNotes">Optional visit or booking reference memo.</param>
public sealed record RedeemPunchPassCreditsCommand(
    Guid IssuedPunchPassId,
    int CreditsToRedeem = 1,
    Guid? RedeemedForPersonId = null,
    Guid? BranchId = null,
    string? ReferenceNotes = null
) : ICommand<IssuedPunchPassResponse>;
