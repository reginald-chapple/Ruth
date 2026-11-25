using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Query to get all households a person belongs to, ordered by HouseholdOrder.</summary>
public sealed record GetPersonHouseholdsQuery(Guid PersonId)
    : IQuery<IReadOnlyList<HouseholdResponse>>;
