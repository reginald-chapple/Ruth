using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Organizations;

/// <summary>Query to retrieve an organization/sponsor profile along with pledged, invoiced, and settled totals.</summary>
/// <param name="OrganizationPersonId">Target CRM Business Person identifier.</param>
public sealed record GetOrganizationByIdQuery(Guid OrganizationPersonId)
    : IQuery<OrganizationDetailResponse>;
