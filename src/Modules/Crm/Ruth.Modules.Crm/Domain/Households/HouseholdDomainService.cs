using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>
/// Pure domain service implementing gender-neutral household salutations, primary/billing contact resolution,
/// and married partner resolution rules.
/// </summary>
public static class HouseholdDomainService
{
    private const int SalutationMaxLength = 250;

    /// <summary>
    /// Resolves the household primary/billing contact using gender-neutral precedence:
    /// Living -> Explicit Primary Contact -> Adult Role -> HouseholdOrder -> Earliest BirthDate -> PersonId.
    /// </summary>
    public static PersonId? ResolvePrimaryContactId(
        Household household,
        IReadOnlyDictionary<PersonId, Person> peopleById
    )
    {
        ArgumentNullException.ThrowIfNull(household);
        ArgumentNullException.ThrowIfNull(peopleById);

        var candidates = household
            .Members.Where(m =>
                m.IsCombinedContributionMember && peopleById.ContainsKey(m.PersonId)
            )
            .Select(m => (Member: m, Person: peopleById[m.PersonId]))
            .OrderBy(static x => x.Person.IsDeceased ? 1 : 0)
            .ThenByDescending(static x => x.Member.IsPrimaryContact)
            .ThenBy(static x => (int)x.Member.Role)
            .ThenBy(static x => x.Member.HouseholdOrder)
            .ThenBy(static x => x.Person.BirthYear ?? int.MaxValue)
            .ThenBy(static x => x.Person.BirthMonth ?? int.MaxValue)
            .ThenBy(static x => x.Person.BirthDay ?? int.MaxValue)
            .ThenBy(static x => x.Person.Id.Value)
            .FirstOrDefault();

        return candidates.Person?.Id;
    }

    /// <summary>Resolves a married adult's spouse/partner within their active households without gender filtering.</summary>
    public static Person? ResolveSpouse(
        Person person,
        IReadOnlyList<Household> households,
        IReadOnlyDictionary<PersonId, Person> peopleById
    )
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(peopleById);

        if (person.MaritalStatus != MaritalStatus.Married)
        {
            return null;
        }

        var candidates = new List<(int HouseholdOrder, int AgeGapDays, Person Candidate)>();
        foreach (var household in households.Where(static h => !h.IsArchived))
        {
            CollectHouseholdSpouseCandidates(person, household, peopleById, candidates);
        }

        return candidates
            .OrderBy(static c => c.HouseholdOrder)
            .ThenBy(static c => c.AgeGapDays)
            .ThenBy(static c => c.Candidate.Id.Value)
            .Select(static c => c.Candidate)
            .FirstOrDefault();
    }

    private static void CollectHouseholdSpouseCandidates(
        Person person,
        Household household,
        IReadOnlyDictionary<PersonId, Person> peopleById,
        List<(int HouseholdOrder, int AgeGapDays, Person Candidate)> candidates
    )
    {
        var selfMember = household.Members.FirstOrDefault(m => m.PersonId == person.Id);
        if (selfMember is null || selfMember.Role != HouseholdRole.Adult)
        {
            return;
        }

        foreach (var otherMember in household.Members)
        {
            if (IsEligibleSpouseCandidate(person, otherMember, peopleById, out var other))
            {
                var gap = ComputeBirthDateGapDays(person.BirthDate, other.BirthDate);
                candidates.Add((selfMember.HouseholdOrder, gap, other));
            }
        }
    }

    private static bool IsEligibleSpouseCandidate(
        Person self,
        HouseholdMember otherMember,
        IReadOnlyDictionary<PersonId, Person> peopleById,
        out Person candidate
    )
    {
        if (
            otherMember.PersonId == self.Id
            || otherMember.Role != HouseholdRole.Adult
            || !peopleById.TryGetValue(otherMember.PersonId, out var resolved)
        )
        {
            candidate = null!;
            return false;
        }

        candidate = resolved;
        return !resolved.IsDeceased && resolved.MaritalStatus == MaritalStatus.Married;
    }

    /// <summary>Computes a concise or full household salutation using primary-contact and member-order sorting.</summary>
    public static string ComputeSalutation(
        Household household,
        IReadOnlyDictionary<PersonId, Person> peopleById,
        bool includeChildren = false,
        bool useFormalNames = false
    )
    {
        ArgumentNullException.ThrowIfNull(household);
        ArgumentNullException.ThrowIfNull(peopleById);

        var allMembers = household
            .Members.Where(m => peopleById.ContainsKey(m.PersonId))
            .Select(m => (Member: m, Person: peopleById[m.PersonId]))
            .ToList();

        var pool = allMembers
            .Where(static x =>
                x.Person.RecordStatus == PersonRecordStatus.Active && !x.Person.IsDeceased
            )
            .ToList();
        if (pool.Count == 0)
        {
            pool = allMembers;
        }

        if (pool.Count == 0)
        {
            return household.Name;
        }

        var adults = pool.Where(static x => x.Member.Role == HouseholdRole.Adult)
            .OrderByDescending(static x => x.Member.IsPrimaryContact)
            .ThenBy(static x => x.Member.HouseholdOrder)
            .ThenBy(static x => x.Person.Id.Value)
            .Select(static x => x.Person)
            .ToList();

        var children = pool.Where(static x => x.Member.Role == HouseholdRole.Child)
            .OrderBy(static x => x.Person.BirthYear ?? int.MaxValue)
            .ThenBy(static x => x.Member.HouseholdOrder)
            .ThenBy(static x => x.Person.Id.Value)
            .Select(static x => x.Person)
            .ToList();

        var selected =
            adults.Count == 0 ? children
            : includeChildren ? [.. adults, .. children]
            : adults;

        var result = FormatSalutationNames(selected, useFormalNames);
        return result.Length > SalutationMaxLength ? result[..SalutationMaxLength] : result;
    }

    /// <summary>Resolves the preferred mailing or mapped home location for a household.</summary>
    public static HouseholdLocation? ResolveHomeLocation(Household household)
    {
        ArgumentNullException.ThrowIfNull(household);

        return household
            .Locations.Where(static l => l.LocationType == HouseholdLocationType.Home)
            .OrderByDescending(static l => l.IsMappedLocation)
            .ThenByDescending(static l => l.Id.Value)
            .FirstOrDefault();
    }

    private static int ComputeBirthDateGapDays(DateOnly? left, DateOnly? right)
    {
        return left.HasValue && right.HasValue
            ? Math.Abs(left.Value.DayNumber - right.Value.DayNumber)
            : int.MaxValue;
    }

    private static string FormatSalutationNames(IReadOnlyList<Person> people, bool formal)
    {
        var distinctLastNames = people
            .Select(static p => p.LastName)
            .Where(static l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctLastNames.Count == 1)
        {
            var firsts = people.Select(p => formal ? p.FirstName : p.NickName).ToList();
            return $"{JoinWithAmpersand(firsts)} {distinctLastNames[0]}".Trim();
        }

        var fullNames = people
            .Select(p => $"{(formal ? p.FirstName : p.NickName)} {p.LastName}".Trim())
            .ToList();
        return JoinWithAmpersand(fullNames);
    }

    private static string JoinWithAmpersand(List<string> items)
    {
        return items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            2 => $"{items[0]} & {items[1]}",
            _ => $"{string.Join(", ", items.Take(items.Count - 1))} & {items[^1]}",
        };
    }
}
