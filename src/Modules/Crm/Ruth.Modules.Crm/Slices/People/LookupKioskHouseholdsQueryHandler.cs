using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class LookupKioskHouseholdsQueryHandler(CrmDbContext dbContext, IClock clock)
    : IQueryHandler<LookupKioskHouseholdsQuery, IReadOnlyList<KioskHouseholdLookupResponse>>
{
    public async Task<Result<IReadOnlyList<KioskHouseholdLookupResponse>>> HandleAsync(
        LookupKioskHouseholdsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (
            string.IsNullOrWhiteSpace(query.SearchTerm)
            && (!query.ExplicitPersonId.HasValue || query.ExplicitPersonId.Value == Guid.Empty)
        )
        {
            return Result.Success<IReadOnlyList<KioskHouseholdLookupResponse>>([]);
        }

        var maxHouseholds = Math.Clamp(query.MaxHouseholds, 1, 25);
        var term = query.SearchTerm?.Trim() ?? string.Empty;
        var (matchedIds, matchKind) =
            query.ExplicitPersonId.HasValue && query.ExplicitPersonId.Value != Guid.Empty
                ? ([PersonId.From(query.ExplicitPersonId.Value)], "WalletPass")
                : await ResolveMatchedPersonIdsAsync(
                    query,
                    term,
                    maxHouseholds * 4,
                    cancellationToken
                );

        if (matchedIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<KioskHouseholdLookupResponse>>([]);
        }

        var responses = await HydrateHouseholdsAsync(
            matchedIds,
            matchKind,
            maxHouseholds,
            cancellationToken
        );
        return Result.Success(responses);
    }

    private async Task<(List<PersonId> PersonIds, string MatchKind)> ResolveMatchedPersonIdsAsync(
        LookupKioskHouseholdsQuery query,
        string term,
        int personLimit,
        CancellationToken cancellationToken
    )
    {
        if (query.AllowBarcodeMatch)
        {
            var barcodeIds = await MatchBySearchKeyAsync(term, personLimit, cancellationToken);
            if (barcodeIds.Count > 0)
            {
                return (barcodeIds, "Barcode");
            }
        }

        var digits = PhoneNumberNormalizer.ExtractDigits(term);
        if (query.AllowPhoneMatch && digits.Length >= 4)
        {
            var phoneIds = await MatchByPhoneDigitsAsync(digits, personLimit, cancellationToken);
            if (phoneIds.Count > 0 || !HasAsciiLetters(term))
            {
                return (phoneIds, "Phone");
            }
        }

        if (!query.AllowNameMatch || term.Length < 2)
        {
            return ([], "None");
        }

        var nameIds = await MatchByNameAsync(term, personLimit, cancellationToken);
        return (nameIds, "Name");
    }

    private Task<List<PersonId>> MatchBySearchKeyAsync(
        string term,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var upper = term.ToUpperInvariant();
        return dbContext
            .PersonSearchKeys.AsNoTracking()
            .Where(k => k.NormalizedSearchValue == upper)
            .Select(static k => k.PersonId)
            .Distinct()
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private Task<List<PersonId>> MatchByPhoneDigitsAsync(
        string digits,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var reversedPrefix = PhoneNumberNormalizer.ReverseDigits(digits);
        return dbContext
            .PhoneNumbers.AsNoTracking()
            .Where(pn => pn.NumberReversed.StartsWith(reversedPrefix) || pn.Number.Contains(digits))
            .Select(static pn => pn.PersonId)
            .Distinct()
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private Task<List<PersonId>> MatchByNameAsync(
        string term,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var tokens = term.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var upper = term.ToUpperInvariant();
        var basePeople = dbContext
            .People.AsNoTracking()
            .Where(static p =>
                !p.IsMerged && !p.IsDeceased && p.RecordType != PersonRecordType.Business
            );

        if (tokens.Length >= 2)
        {
            var firstUpper = tokens[0].ToUpperInvariant();
            var lastUpper = tokens[^1].ToUpperInvariant();
            return basePeople
                .Where(p =>
                    (
                        p.NormalizedFirstName.StartsWith(firstUpper)
                        || p.NormalizedNickName.StartsWith(firstUpper)
                    ) && p.NormalizedLastName.StartsWith(lastUpper)
                )
                .OrderBy(static p => p.NormalizedLastName)
                .ThenBy(static p => p.NormalizedNickName)
                .Select(static p => p.Id)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        return basePeople
            .Where(p =>
                p.NormalizedLastName.StartsWith(upper)
                || p.NormalizedNickName.StartsWith(upper)
                || p.NormalizedFirstName.StartsWith(upper)
            )
            .OrderBy(static p => p.NormalizedLastName)
            .ThenBy(static p => p.NormalizedNickName)
            .Select(static p => p.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<KioskHouseholdLookupResponse>> HydrateHouseholdsAsync(
        List<PersonId> matchedPersonIds,
        string matchKind,
        int maxHouseholds,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow.ToUniversalTime();
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        var householdIds = await dbContext
            .HouseholdMembers.AsNoTracking()
            .Where(m => matchedPersonIds.Contains(m.PersonId))
            .OrderBy(static m => m.HouseholdOrder)
            .Select(static m => m.HouseholdId)
            .Distinct()
            .Take(maxHouseholds)
            .ToListAsync(cancellationToken);

        var households =
            householdIds.Count == 0
                ? []
                : await dbContext
                    .Households.AsNoTracking()
                    .Include(static h => h.Members)
                    .Where(h => householdIds.Contains(h.Id) && !h.IsArchived && h.IsActive)
                    .ToListAsync(cancellationToken);

        var allPersonIds = matchedPersonIds
            .Concat(households.SelectMany(static h => h.Members.Select(static m => m.PersonId)))
            .Distinct()
            .ToArray();

        var peopleById = await dbContext
            .People.AsNoTracking()
            .Include(static p => p.PhoneNumbers)
            .Where(p =>
                allPersonIds.Contains(p.Id)
                && !p.IsMerged
                && !p.IsDeceased
                && p.RecordType != PersonRecordType.Business
            )
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        var activeSignalsByPerson = await LoadActiveSignalsAsync(
            allPersonIds,
            nowUtc,
            cancellationToken
        );

        return BuildHouseholdResponses(
            households,
            matchedPersonIds,
            peopleById,
            activeSignalsByPerson,
            matchKind,
            today,
            maxHouseholds
        );
    }

    private async Task<Dictionary<PersonId, List<string>>> LoadActiveSignalsAsync(
        PersonId[] personIds,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var signals = await dbContext
            .PersonSignals.AsNoTracking()
            .Where(s =>
                personIds.Contains(s.PersonId)
                && (!s.ExpirationDateUtc.HasValue || s.ExpirationDateUtc.Value >= nowUtc)
            )
            .Join(
                dbContext.SignalTypes.AsNoTracking(),
                static s => s.SignalTypeId,
                static t => t.Id,
                static (s, t) =>
                    new
                    {
                        s.PersonId,
                        t.Order,
                        Banner = string.IsNullOrWhiteSpace(s.Note) ? t.Name : $"{t.Name}: {s.Note}",
                    }
            )
            .OrderBy(static x => x.Order)
            .ToListAsync(cancellationToken);

        var map = new Dictionary<PersonId, List<string>>();
        foreach (var item in signals)
        {
            if (!map.TryGetValue(item.PersonId, out var list))
            {
                list = [];
                map[item.PersonId] = list;
            }

            list.Add(item.Banner);
        }

        return map;
    }

    private static List<KioskHouseholdLookupResponse> BuildHouseholdResponses(
        List<Household> households,
        List<PersonId> matchedPersonIds,
        Dictionary<PersonId, Person> peopleById,
        Dictionary<PersonId, List<string>> signalsByPerson,
        string matchKind,
        DateOnly today,
        int maxHouseholds
    )
    {
        var matchedSet = matchedPersonIds.Select(static p => p.Value).ToHashSet();
        var coveredPeople = new HashSet<PersonId>();
        var output = new List<KioskHouseholdLookupResponse>(maxHouseholds);

        foreach (var household in households)
        {
            var members = household
                .Members.Where(m => peopleById.ContainsKey(m.PersonId))
                .OrderBy(static m => m.Role == HouseholdRole.Adult ? 0 : 1)
                .Select(m =>
                {
                    _ = coveredPeople.Add(m.PersonId);
                    return MapPerson(
                        peopleById[m.PersonId],
                        m.Role == HouseholdRole.Child,
                        signalsByPerson,
                        today
                    );
                })
                .ToList();

            if (members.Count == 0)
            {
                continue;
            }

            var matchedInHousehold = members
                .Where(m => matchedSet.Contains(m.PersonId))
                .Select(static m => m.PersonId)
                .ToList();

            output.Add(
                new KioskHouseholdLookupResponse(
                    household.Id.Value,
                    household.Name,
                    household.BranchId,
                    matchKind,
                    matchedInHousehold,
                    members
                )
            );
        }

        AppendStandalonePeople(
            output,
            matchedPersonIds,
            coveredPeople,
            peopleById,
            signalsByPerson,
            matchKind,
            today,
            maxHouseholds
        );
        return output;
    }

    private static void AppendStandalonePeople(
        List<KioskHouseholdLookupResponse> output,
        List<PersonId> matchedPersonIds,
        HashSet<PersonId> coveredPeople,
        Dictionary<PersonId, Person> peopleById,
        Dictionary<PersonId, List<string>> signalsByPerson,
        string matchKind,
        DateOnly today,
        int maxHouseholds
    )
    {
        foreach (var personId in matchedPersonIds)
        {
            if (output.Count >= maxHouseholds)
            {
                break;
            }

            if (
                coveredPeople.Contains(personId)
                || !peopleById.TryGetValue(personId, out var person)
            )
            {
                continue;
            }

            var dto = MapPerson(
                person,
                person.AgeClassification == AgeClassification.Child,
                signalsByPerson,
                today
            );
            output.Add(
                new KioskHouseholdLookupResponse(
                    person.PrimaryHouseholdId ?? person.Id.Value,
                    $"{person.LastName} Household",
                    person.PrimaryBranchId,
                    matchKind,
                    [person.Id.Value],
                    [dto]
                )
            );
        }
    }

    private static KioskPersonLookupDto MapPerson(
        Person person,
        bool isChildRole,
        Dictionary<PersonId, List<string>> signalsByPerson,
        DateOnly today
    )
    {
        var ageYears = ComputeExactAgeYears(person, today);
        var gradeOffset = PersonDemographicsCalculator.CalculateGradeOffset(
            person.GraduationYear,
            today
        );
        int? numericGrade = gradeOffset.HasValue ? 12 - gradeOffset.Value : null;
        var primaryPhone = person
            .PhoneNumbers.OrderBy(static p => (int)p.NumberType)
            .FirstOrDefault();

        var tier = ReadAttribute(person.Attributes, "MembershipTier", "Tier");
        var skillsCsv = ReadAttribute(person.Attributes, "Skills", "SkillCodes", "BadgeCodes");
        var allergy = ReadAttribute(person.Attributes, "Allergies", "MedicalAlert", "MedicalNotes");
        var needsAccommodations = ParseBoolAttribute(
            ReadAttribute(person.Attributes, "NeedsAccommodations", "SpecialNeeds")
        );

        return new KioskPersonLookupDto(
            person.Id.Value,
            person.FirstName,
            person.NickName,
            person.LastName,
            person.FullName,
            ageYears,
            person.BirthDate,
            numericGrade,
            PersonDemographicsCalculator.FormatGrade(gradeOffset),
            primaryPhone?.NumberFormatted,
            isChildRole || person.AgeClassification == AgeClassification.Child,
            tier,
            SplitCsvTokens(skillsCsv),
            needsAccommodations,
            allergy,
            signalsByPerson.TryGetValue(person.Id, out var notes) ? notes : []
        );
    }

    private static decimal? ComputeExactAgeYears(Person person, DateOnly today)
    {
        if (!person.BirthYear.HasValue || !person.BirthMonth.HasValue || !person.BirthDay.HasValue)
        {
            return null;
        }

        var wholeYears = PersonDemographicsCalculator.CalculateAge(
            person.BirthYear,
            person.BirthMonth,
            person.BirthDay,
            today,
            person.DeceasedDate
        );
        if (!wholeYears.HasValue || !person.BirthDate.HasValue)
        {
            return wholeYears;
        }

        var lastBirthday = person.BirthDate.Value.AddYears(wholeYears.Value);
        var daysSinceBirthday = Math.Max(0, today.DayNumber - lastBirthday.DayNumber);
        return Math.Round(
            wholeYears.Value + (daysSinceBirthday / 365.25m),
            2,
            MidpointRounding.AwayFromZero
        );
    }

    private static string? ReadAttribute(
        IReadOnlyDictionary<string, string?> attributes,
        params ReadOnlySpan<string> keys
    )
    {
        foreach (var key in keys)
        {
            if (attributes.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }
        }

        return null;
    }

    private static bool ParseBoolAttribute(string? raw)
    {
        return !string.IsNullOrWhiteSpace(raw)
            && (
                (bool.TryParse(raw, out var flag) && flag)
                || string.Equals(raw, "1", StringComparison.Ordinal)
                || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string[] SplitCsvTokens(string? csv)
    {
        return string.IsNullOrWhiteSpace(csv)
            ? []
            : csv.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }

    private static bool HasAsciiLetters(string input)
    {
        foreach (var ch in input)
        {
            if (char.IsLetter(ch))
            {
                return true;
            }
        }

        return false;
    }
}
