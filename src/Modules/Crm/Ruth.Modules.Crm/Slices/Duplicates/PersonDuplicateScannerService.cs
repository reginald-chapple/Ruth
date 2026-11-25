using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class PersonDuplicateScannerService(CrmDbContext dbContext, IClock clock)
{
    public async Task<DuplicateScanSummaryResponse> ScanAsync(
        bool includeInactive,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var profiles = await LoadCandidateProfilesAsync(includeInactive, cancellationToken);
        var existingPairs = await dbContext.PersonDuplicates.ToDictionaryAsync(
            static d => (d.Person1Id, d.Person2Id),
            cancellationToken
        );

        var matchedKeys = new HashSet<(PersonId, PersonId)>();
        var created = 0;
        var updated = 0;

        for (var i = 0; i < profiles.Count; i++)
        {
            for (var j = i + 1; j < profiles.Count; j++)
            {
                var evaluation = PersonDuplicateScorer.Evaluate(profiles[i], profiles[j]);
                if (!evaluation.IsCandidateMatch)
                {
                    continue;
                }

                var key = PersonDuplicate.OrderPair(profiles[i].PersonId, profiles[j].PersonId);
                _ = matchedKeys.Add(key);

                if (existingPairs.TryGetValue(key, out var existing))
                {
                    existing.UpdateFromScan(evaluation, nowUtc);
                    updated++;
                }
                else
                {
                    var added = PersonDuplicate.Create(key.First, key.Second, evaluation, nowUtc);
                    _ = dbContext.PersonDuplicates.Add(added);
                    existingPairs[key] = added;
                    created++;
                }
            }
        }

        var stale = existingPairs
            .Where(pair => !matchedKeys.Contains(pair.Key) && !pair.Value.IsConfirmedAsNotDuplicate)
            .Select(static pair => pair.Value)
            .ToList();

        if (stale.Count > 0)
        {
            dbContext.PersonDuplicates.RemoveRange(stale);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new DuplicateScanSummaryResponse(
            profiles.Count,
            created,
            updated,
            stale.Count,
            nowUtc
        );
    }

    private async Task<List<DuplicateCandidateProfile>> LoadCandidateProfilesAsync(
        bool includeInactive,
        CancellationToken cancellationToken
    )
    {
        var peopleQuery = dbContext
            .People.AsNoTracking()
            .Include(static p => p.PhoneNumbers)
            .Where(static p => !p.IsMerged && p.RecordType == PersonRecordType.Person);

        if (!includeInactive)
        {
            peopleQuery = peopleQuery.Where(static p =>
                p.RecordStatus != PersonRecordStatus.Inactive
            );
        }

        var people = await peopleQuery.ToListAsync(cancellationToken);
        var households = await dbContext
            .Households.AsNoTracking()
            .AsSplitQuery()
            .Include(static h => h.Members)
            .Include(static h => h.Locations)
            .Where(static h => !h.IsArchived)
            .ToListAsync(cancellationToken);

        var nickMap = await BuildNicknameMapAsync(cancellationToken);
        var householdByPerson = BuildPrimaryHouseholdLookup(households);

        return [.. people.Select(p => BuildProfile(p, householdByPerson, nickMap))];
    }

    private async Task<Dictionary<string, HashSet<string>>> BuildNicknameMapAsync(
        CancellationToken cancellationToken
    )
    {
        var rows = await dbContext
            .MetaNickNameLookups.AsNoTracking()
            .ToListAsync(cancellationToken);
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            AddBidirectional(map, row.FirstName, row.NickName);
            AddBidirectional(map, row.NickName, row.FirstName);
        }

        return map;
    }

    private static void AddBidirectional(
        Dictionary<string, HashSet<string>> map,
        string key,
        string value
    )
    {
        if (!map.TryGetValue(key, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            map[key] = set;
        }

        _ = set.Add(value);
    }

    private static Dictionary<
        PersonId,
        (Household Household, HouseholdMember Member)
    > BuildPrimaryHouseholdLookup(List<Household> households)
    {
        var lookup = new Dictionary<PersonId, (Household Household, HouseholdMember Member)>();
        foreach (var household in households)
        {
            foreach (var member in household.Members)
            {
                if (
                    !lookup.TryGetValue(member.PersonId, out var existing)
                    || member.HouseholdOrder < existing.Member.HouseholdOrder
                )
                {
                    lookup[member.PersonId] = (household, member);
                }
            }
        }

        return lookup;
    }

    private static DuplicateCandidateProfile BuildProfile(
        Person person,
        Dictionary<PersonId, (Household Household, HouseholdMember Member)> householdByPerson,
        Dictionary<string, HashSet<string>> nickMap
    )
    {
        _ = householdByPerson.TryGetValue(person.Id, out var hh);
        var homeLocation = hh.Household is null
            ? null
            : HouseholdDomainService.ResolveHomeLocation(hh.Household);

        var cellPhones = person
            .PhoneNumbers.Where(static pn => pn.NumberType == PhoneNumberType.Mobile)
            .Select(static pn => $"{pn.CountryCode}:{pn.Number}:{pn.Extension ?? string.Empty}")
            .ToList();

        var homePhones = person
            .PhoneNumbers.Where(static pn => pn.NumberType != PhoneNumberType.Mobile)
            .Select(static pn => $"{pn.CountryCode}:{pn.Number}:{pn.Extension ?? string.Empty}")
            .ToList();

        var expanded = ExpandFirstNames(person.FirstName, person.NickName, nickMap);
        return new DuplicateCandidateProfile(
            person.Id,
            person.FirstName,
            person.NickName,
            person.LastName,
            person.Suffix,
            person.NormalizedEmail,
            person.Gender,
            person.MaritalStatus,
            person.BirthDate,
            person.PrimaryBranchId,
            person.PrimaryHouseholdId ?? hh.Household?.Id.Value,
            hh.Member?.Role,
            homeLocation?.LocationId,
            cellPhones,
            homePhones,
            DoubleMetaphoneEncoder.Encode(person.FirstName).PrimaryKey,
            DoubleMetaphoneEncoder.Encode(person.LastName).PrimaryKey,
            expanded
        );
    }

    private static HashSet<string> ExpandFirstNames(
        string firstName,
        string nickName,
        Dictionary<string, HashSet<string>> nickMap
    )
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(firstName))
        {
            _ = set.Add(firstName);
            if (nickMap.TryGetValue(firstName, out var variants))
            {
                set.UnionWith(variants);
            }
        }

        if (!string.IsNullOrWhiteSpace(nickName))
        {
            _ = set.Add(nickName);
            if (nickMap.TryGetValue(nickName, out var variants))
            {
                set.UnionWith(variants);
            }
        }

        return set;
    }
}
