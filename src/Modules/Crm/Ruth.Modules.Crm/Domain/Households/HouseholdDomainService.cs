using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Domain.Households;

/// <summary>
/// Pure domain service implementing Rock RMS household salutations, giving-leader precedence,
/// and spouse resolution rules.
/// </summary>
public static class HouseholdDomainService
{
    private const int SalutationMaxLength = 250;

    /// <summary>Resolves the household Giving Leader using Rock's exact precedence: Living -> Adult -> Gender -> Oldest.</summary>
    public static PersonId? ResolveGivingLeaderId(
        Household household,
        IReadOnlyDictionary<PersonId, Person> peopleById
    )
    {
        ArgumentNullException.ThrowIfNull(household);
        ArgumentNullException.ThrowIfNull(peopleById);

        var candidates = household
            .Members.Where(m => m.IsCombinedGivingMember && peopleById.ContainsKey(m.PersonId))
            .Select(m => (Member: m, Person: peopleById[m.PersonId]))
            .OrderBy(static x => x.Person.IsDeceased ? 1 : 0)
            .ThenBy(static x => (int)x.Member.Role)
            .ThenBy(static x => (int)x.Person.Gender)
            .ThenBy(static x => x.Person.BirthYear ?? int.MaxValue)
            .ThenBy(static x => x.Person.BirthMonth ?? int.MaxValue)
            .ThenBy(static x => x.Person.BirthDay ?? int.MaxValue)
            .ThenBy(static x => x.Person.Id.Value)
            .FirstOrDefault();

        return candidates.Person?.Id;
    }

    /// <summary>Resolves a person's spouse within their households matching Rock RMS marital and adult rules.</summary>
    public static Person? ResolveSpouse(
        Person person,
        IReadOnlyList<Household> households,
        IReadOnlyDictionary<PersonId, Person> peopleById,
        bool strictOppositeGender = true
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
            var selfMember = household.Members.FirstOrDefault(m => m.PersonId == person.Id);
            if (selfMember is null || selfMember.Role != HouseholdRole.Adult)
            {
                continue;
            }

            foreach (var otherMember in household.Members)
            {
                if (
                    otherMember.PersonId == person.Id
                    || otherMember.Role != HouseholdRole.Adult
                    || !peopleById.TryGetValue(otherMember.PersonId, out var other)
                )
                {
                    continue;
                }

                if (!IsEligibleSpouse(person, other, strictOppositeGender))
                {
                    continue;
                }

                var gap = ComputeBirthDateGapDays(person.BirthDate, other.BirthDate);
                candidates.Add((selfMember.HouseholdOrder, gap, other));
            }
        }

        return candidates
            .OrderBy(static c => c.HouseholdOrder)
            .ThenBy(static c => c.AgeGapDays)
            .ThenBy(static c => c.Candidate.Id.Value)
            .Select(static c => c.Candidate)
            .FirstOrDefault();
    }

    /// <summary>Computes a concise or full household salutation matching Rock's shared-last-name rules.</summary>
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
            .Where(static x => x.Person.RecordStatus == PersonRecordStatus.Active)
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
            .OrderBy(static x => (int)x.Person.Gender)
            .ThenBy(static x => x.Person.Id.Value)
            .Select(static x => x.Person)
            .ToList();

        var children = pool.Where(static x => x.Member.Role == HouseholdRole.Child)
            .OrderBy(static x => x.Person.BirthYear ?? int.MaxValue)
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

    private static bool IsEligibleSpouse(Person person, Person other, bool strictOppositeGender)
    {
        return !other.IsDeceased
            && other.MaritalStatus == MaritalStatus.Married
            && (
                !strictOppositeGender
                || person.Gender == Gender.Unknown
                || other.Gender == Gender.Unknown
                || person.Gender != other.Gender
            );
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
