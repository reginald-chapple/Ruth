using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class StatementRecipientResolver(IQueryDispatcher queryDispatcher)
{
    public async Task<
        Result<(
            string DisplayName,
            HashSet<Guid> MemberPersonIds,
            Guid? HouseholdId,
            Guid? PrimaryPersonId
        )>
    > ResolveAsync(
        FinancialStatementRecipientScope scope,
        Guid? targetPersonId,
        Guid? targetHouseholdId,
        CancellationToken cancellationToken
    )
    {
        if (scope == FinancialStatementRecipientScope.Household)
        {
            return await ResolveHouseholdAsync(targetHouseholdId, cancellationToken);
        }

        if (targetPersonId is null || targetPersonId.Value == Guid.Empty)
        {
            return FinanceErrors.StatementRecipientInvalid;
        }

        var personId = targetPersonId.Value;
        var personRes = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId),
            cancellationToken
        );
        var displayName = personRes.IsSuccess
            ? personRes.Value.FullName
            : $"Constituent {personId.ToString("D")[..8]}";

        return (
            displayName,
            new HashSet<Guid> { personId },
            targetHouseholdId is { } hh && hh != Guid.Empty ? hh : null,
            personId
        );
    }

    public async Task<HashSet<Guid>> ResolveHouseholdMemberIdsAsync(
        Guid householdId,
        CancellationToken cancellationToken
    )
    {
        var members = new HashSet<Guid>();
        var hhRes = await queryDispatcher.QueryAsync<GetHouseholdByIdQuery, HouseholdResponse>(
            new GetHouseholdByIdQuery(householdId),
            cancellationToken
        );
        if (hhRes.IsSuccess)
        {
            foreach (var member in hhRes.Value.Members)
            {
                _ = members.Add(member.PersonId);
            }
        }

        return members;
    }

    public async Task<(
        bool IsBusiness,
        HouseholdResponse? CombinedHousehold
    )> InspectPersonForBatchAsync(
        Guid personId,
        bool consolidateHouseholds,
        CancellationToken cancellationToken
    )
    {
        var personRes = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId),
            cancellationToken
        );
        if (personRes.IsSuccess && personRes.Value.RecordType == PersonRecordType.Business)
        {
            return (true, null);
        }

        if (!consolidateHouseholds)
        {
            return (false, null);
        }

        var hhList = await queryDispatcher.QueryAsync<
            GetPersonHouseholdsQuery,
            IReadOnlyList<HouseholdResponse>
        >(new GetPersonHouseholdsQuery(personId), cancellationToken);
        var combinedHh = hhList.IsSuccess
            ? hhList.Value.FirstOrDefault(static h => h.CombineContributionsByDefault && h.IsActive)
            : null;

        return (false, combinedHh);
    }

    private async Task<
        Result<(
            string DisplayName,
            HashSet<Guid> MemberPersonIds,
            Guid? HouseholdId,
            Guid? PrimaryPersonId
        )>
    > ResolveHouseholdAsync(Guid? targetHouseholdId, CancellationToken cancellationToken)
    {
        if (targetHouseholdId is null || targetHouseholdId.Value == Guid.Empty)
        {
            return FinanceErrors.StatementRecipientInvalid;
        }

        var hhId = targetHouseholdId.Value;
        var hhRes = await queryDispatcher.QueryAsync<GetHouseholdByIdQuery, HouseholdResponse>(
            new GetHouseholdByIdQuery(hhId),
            cancellationToken
        );

        var members = new HashSet<Guid>();
        var displayName = $"Household {hhId.ToString("D")[..8]}";
        Guid? primaryPerson = null;

        if (hhRes.IsSuccess)
        {
            displayName = string.IsNullOrWhiteSpace(hhRes.Value.SalutationFull)
                ? hhRes.Value.Name
                : hhRes.Value.SalutationFull;
            primaryPerson = hhRes.Value.PrimaryContactPersonId;
            foreach (var member in hhRes.Value.Members)
            {
                _ = members.Add(member.PersonId);
            }
        }

        return (displayName, members, hhId, primaryPerson);
    }
}
