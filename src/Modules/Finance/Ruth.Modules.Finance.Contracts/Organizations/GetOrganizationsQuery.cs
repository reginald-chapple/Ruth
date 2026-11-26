using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Query to search and list registered organizations and corporate sponsors.</summary>
/// <param name="SearchTerm">Optional name, email, or search-key query string.</param>
/// <param name="BranchId">Optional Branch/Site filter.</param>
/// <param name="MaxResults">Maximum number of records to return (1..200).</param>
public sealed record GetOrganizationsQuery(
    string? SearchTerm = null,
    Guid? BranchId = null,
    int MaxResults = 50
) : IQuery<IReadOnlyList<OrganizationSummaryResponse>>;
