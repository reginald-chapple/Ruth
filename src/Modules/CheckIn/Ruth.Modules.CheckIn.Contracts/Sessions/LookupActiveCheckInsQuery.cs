using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Looks up active checked-in attendance entries for check-out by pickup code, person, or household.</summary>
/// <param name="ConfigurationId">Optional configuration filter.</param>
/// <param name="SecurityPickupCode">Optional daily security pickup code.</param>
/// <param name="PersonId">Optional participant person identifier.</param>
/// <param name="HouseholdId">Optional household identifier.</param>
/// <param name="ServiceDate">Optional local service date (defaults to current UTC date).</param>
public sealed record LookupActiveCheckInsQuery(
    Guid? ConfigurationId = null,
    string? SecurityPickupCode = null,
    Guid? PersonId = null,
    Guid? HouseholdId = null,
    DateOnly? ServiceDate = null
) : IQuery<IReadOnlyList<CheckedInEntryReceiptDto>>;
