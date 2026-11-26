using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Organizations;

internal sealed class GetOrganizationsQueryHandler(IQueryDispatcher queryDispatcher)
    : IQueryHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<OrganizationSummaryResponse>>> HandleAsync(
        GetOrganizationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var clampedMax = Math.Clamp(query.MaxResults, 1, 200);
        var crmResult = await queryDispatcher.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(
            new SearchPeopleQuery(
                Query: query.SearchTerm,
                PrimaryBranchId: query.BranchId,
                IncludeBusinesses: true,
                MaxResults: Math.Min(clampedMax * 3, 200)
            ),
            cancellationToken
        );
        if (crmResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<OrganizationSummaryResponse>>(crmResult.Errors);
        }

        var businesses = crmResult
            .Value.Where(static p => p.RecordType == PersonRecordType.Business)
            .Take(clampedMax)
            .ToArray();

        return Result.Success(await HydrateSummariesAsync(businesses, cancellationToken));
    }

    private async Task<IReadOnlyList<OrganizationSummaryResponse>> HydrateSummariesAsync(
        PersonSummaryResponse[] businesses,
        CancellationToken cancellationToken
    )
    {
        var summaries = new List<OrganizationSummaryResponse>(businesses.Length);
        foreach (var item in businesses)
        {
            var detail = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(item.Id),
                cancellationToken
            );
            var attrs = detail.IsSuccess
                ? detail.Value.Attributes
                : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var orgName = !string.IsNullOrWhiteSpace(item.LastName) ? item.LastName : item.FullName;

            summaries.Add(
                new OrganizationSummaryResponse(
                    item.Id,
                    orgName,
                    item.Email,
                    item.PrimaryPhoneFormatted,
                    item.PrimaryBranchId,
                    OrganizationCrmAttributeKeys.GetValue(
                        attrs,
                        OrganizationCrmAttributeKeys.TaxIdentifier
                    ),
                    OrganizationCrmAttributeKeys.GetValue(
                        attrs,
                        OrganizationCrmAttributeKeys.SponsorTier
                    ),
                    OrganizationCrmAttributeKeys.GetValue(
                        attrs,
                        OrganizationCrmAttributeKeys.WebsiteUrl
                    ),
                    item.RecordStatus == PersonRecordStatus.Active
                )
            );
        }

        return summaries;
    }
}
