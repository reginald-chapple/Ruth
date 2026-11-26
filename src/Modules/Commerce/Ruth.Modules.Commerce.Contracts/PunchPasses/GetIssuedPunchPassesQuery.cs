using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Queries issued punch passes filtered by owner person or active status.</summary>
/// <param name="OwnerPersonId">Optional owner CRM person identifier.</param>
/// <param name="ActiveOnly">When true, returns only active non-expired passes with remaining credits.</param>
public sealed record GetIssuedPunchPassesQuery(Guid? OwnerPersonId = null, bool ActiveOnly = false)
    : IQuery<IReadOnlyList<IssuedPunchPassResponse>>;
