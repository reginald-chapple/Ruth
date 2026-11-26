using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Issues a complimentary or staff-granted punch pass to a constituent and syncs Smart Wallet Passes.</summary>
/// <param name="PunchPassTypeId">Target PunchPassType identifier.</param>
/// <param name="OwnerPersonId">Constituent CRM person identifier.</param>
/// <param name="ReferenceNotes">Optional issuance reason.</param>
public sealed record IssuePunchPassCommand(
    Guid PunchPassTypeId,
    Guid OwnerPersonId,
    string? ReferenceNotes = null
) : ICommand<IssuedPunchPassResponse>;
