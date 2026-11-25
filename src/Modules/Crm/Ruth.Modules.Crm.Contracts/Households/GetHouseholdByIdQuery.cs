using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Query to retrieve a single household by identifier.</summary>
public sealed record GetHouseholdByIdQuery(Guid HouseholdId) : IQuery<HouseholdResponse>;
