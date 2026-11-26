using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices;

internal sealed class MemberEligibilityResolver(IQueryDispatcher queryDispatcher)
{
    public async Task<ResolvedMemberProfile> ResolveAsync(
        Guid? personId,
        string? explicitMembershipTier,
        CancellationToken cancellationToken
    )
    {
        var tiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupIds = new HashSet<Guid>();
        var groupTypeIds = new HashSet<Guid>();

        if (!string.IsNullOrWhiteSpace(explicitMembershipTier))
        {
            _ = tiers.Add(explicitMembershipTier.Trim());
        }

        if (personId is null || personId == Guid.Empty)
        {
            return new ResolvedMemberProfile(tiers, groupIds, groupTypeIds);
        }

        await PopulatePersonAttributeTiersAsync(personId.Value, tiers, cancellationToken);
        await PopulateGroupMembershipsAsync(
            personId.Value,
            groupIds,
            groupTypeIds,
            cancellationToken
        );

        return new ResolvedMemberProfile(tiers, groupIds, groupTypeIds);
    }

    public async Task<bool> AreInSameHouseholdAsync(
        Guid ownerPersonId,
        Guid beneficiaryPersonId,
        CancellationToken cancellationToken
    )
    {
        if (ownerPersonId == beneficiaryPersonId)
        {
            return true;
        }

        var res = await queryDispatcher.QueryAsync<
            GetPersonHouseholdsQuery,
            IReadOnlyList<HouseholdResponse>
        >(new GetPersonHouseholdsQuery(ownerPersonId), cancellationToken);
        return !res.IsFailure
            && res.Value.Any(h =>
                h.IsActive && !h.IsArchived && h.Members.Any(m => m.PersonId == beneficiaryPersonId)
            );
    }

    private async Task PopulatePersonAttributeTiersAsync(
        Guid personId,
        HashSet<string> tiers,
        CancellationToken cancellationToken
    )
    {
        var personRes = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId, FollowRedirects: true),
            cancellationToken
        );
        if (personRes.IsFailure)
        {
            return;
        }

        foreach (var key in new[] { "MembershipTier", "Tier" })
        {
            if (
                personRes.Value.Attributes.TryGetValue(key, out var raw)
                && !string.IsNullOrWhiteSpace(raw)
            )
            {
                _ = tiers.Add(raw.Trim());
            }
        }
    }

    private async Task PopulateGroupMembershipsAsync(
        Guid personId,
        HashSet<Guid> groupIds,
        HashSet<Guid> groupTypeIds,
        CancellationToken cancellationToken
    )
    {
        var groupsRes = await queryDispatcher.QueryAsync<
            GetPersonGroupMembershipsQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetPersonGroupMembershipsQuery(personId, ActiveOnly: true), cancellationToken);

        if (groupsRes.IsFailure)
        {
            return;
        }

        foreach (var member in groupsRes.Value)
        {
            _ = groupIds.Add(member.GroupId);
            _ = groupTypeIds.Add(member.GroupTypeId);
        }
    }
}
