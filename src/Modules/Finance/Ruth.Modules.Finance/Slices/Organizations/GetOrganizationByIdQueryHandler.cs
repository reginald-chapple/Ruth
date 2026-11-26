using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Slices.Pledges;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Organizations;

internal sealed class GetOrganizationByIdQueryHandler(
    IQueryDispatcher queryDispatcher,
    FinanceDbContext dbContext,
    IFinanceLedgerSummaryReader ledgerSummaryReader
) : IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse>
{
    public async Task<Result<OrganizationDetailResponse>> HandleAsync(
        GetOrganizationByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var personResult = await queryDispatcher.QueryAsync<
            GetPersonByIdQuery,
            PersonDetailResponse
        >(new GetPersonByIdQuery(query.OrganizationPersonId), cancellationToken);
        if (personResult.IsFailure || personResult.Value.RecordType != PersonRecordType.Business)
        {
            return FinanceErrors.OrganizationNotFound;
        }

        var person = personResult.Value;
        var activePledged = await dbContext
            .FinancialPledges.AsNoTracking()
            .Where(p => p.PersonId == person.Id && p.Status == FinancialPledgeStatus.Active)
            .SumAsync(static p => (decimal?)p.TotalAmount, cancellationToken)
            ?? 0m;

        var (openInvoices, lifetimeSettled) = await ledgerSummaryReader.GetConstituentTotalsAsync(
            person.Id,
            cancellationToken
        );

        var primaryPhone =
            person.PhoneNumbers.Count > 0 ? person.PhoneNumbers[0].NumberFormatted : null;
        var orgName = !string.IsNullOrWhiteSpace(person.LastName)
            ? person.LastName
            : person.FullName;

        return new OrganizationDetailResponse(
            person.Id,
            orgName,
            person.Email,
            primaryPhone,
            person.PrimaryBranchId,
            OrganizationCrmAttributeKeys.GetValue(
                person.Attributes,
                OrganizationCrmAttributeKeys.TaxIdentifier
            ),
            OrganizationCrmAttributeKeys.GetValue(
                person.Attributes,
                OrganizationCrmAttributeKeys.SponsorTier
            ),
            OrganizationCrmAttributeKeys.GetValue(
                person.Attributes,
                OrganizationCrmAttributeKeys.WebsiteUrl
            ),
            OrganizationCrmAttributeKeys.GetValue(
                person.Attributes,
                OrganizationCrmAttributeKeys.BillingNotes
            ),
            person.RecordStatus == PersonRecordStatus.Active,
            activePledged,
            openInvoices,
            lifetimeSettled,
            person.CreatedOnUtc
        );
    }
}
