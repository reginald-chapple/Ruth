using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class SearchPeopleQueryHandler(CrmDbContext dbContext, IClock clock)
    : IQueryHandler<SearchPeopleQuery, IReadOnlyList<PersonSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<PersonSummaryResponse>>> HandleAsync(
        SearchPeopleQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var limit = Math.Clamp(query.MaxResults, 1, 100);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var baseQuery = BuildFilteredBaseQuery(query);

        if (string.IsNullOrWhiteSpace(query.Query))
        {
            var directory = await baseQuery
                .OrderBy(static p => p.LastName)
                .ThenBy(static p => p.NickName)
                .Take(limit)
                .ToListAsync(cancellationToken);
            return Result.Success<IReadOnlyList<PersonSummaryResponse>>([
                .. directory.Select(p => PersonMapper.ToSummary(p, today, "Directory")),
            ]);
        }

        var term = query.Query.Trim();
        var ranked = new Dictionary<PersonId, (Person Person, int Priority, string Reason)>();

        await AddExactKeyAndPhoneMatchesAsync(baseQuery, term, limit, ranked, cancellationToken);
        await AddTrigramNameMatchesAsync(baseQuery, term, limit, ranked, cancellationToken);
        await AddPhoneticAndNicknameMatchesAsync(baseQuery, term, limit, ranked, cancellationToken);

        IReadOnlyList<PersonSummaryResponse> results =
        [
            .. ranked
                .Values.OrderBy(static x => x.Priority)
                .ThenBy(static x => x.Person.LastName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static x => x.Person.NickName, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .Select(x => PersonMapper.ToSummary(x.Person, today, x.Reason)),
        ];

        return Result.Success(results);
    }

    private IQueryable<Person> BuildFilteredBaseQuery(SearchPeopleQuery query)
    {
        var q = dbContext
            .People.AsNoTracking()
            .Include(static p => p.PhoneNumbers)
            .Where(static p => !p.IsMerged);

        if (!query.IncludeBusinesses)
        {
            q = q.Where(static p => p.RecordType != PersonRecordType.Business);
        }

        if (!query.IncludeDeceased)
        {
            q = q.Where(static p => !p.IsDeceased);
        }

        if (query.RecordStatus.HasValue)
        {
            q = q.Where(p => p.RecordStatus == query.RecordStatus.Value);
        }

        if (query.ConnectionStatus.HasValue)
        {
            q = q.Where(p => p.ConnectionStatus == query.ConnectionStatus.Value);
        }

        if (query.PrimaryBranchId.HasValue)
        {
            q = q.Where(p => p.PrimaryBranchId == query.PrimaryBranchId.Value);
        }

        return q;
    }

    private async Task AddExactKeyAndPhoneMatchesAsync(
        IQueryable<Person> baseQuery,
        string term,
        int limit,
        Dictionary<PersonId, (Person Person, int Priority, string Reason)> ranked,
        CancellationToken cancellationToken
    )
    {
        var upper = term.ToUpperInvariant();
        var keyPersonIds = await dbContext
            .PersonSearchKeys.AsNoTracking()
            .Where(k => k.NormalizedSearchValue == upper)
            .Select(static k => k.PersonId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (keyPersonIds.Count > 0)
        {
            var matched = await baseQuery
                .Where(p => keyPersonIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            RecordMatches(ranked, matched, priority: 1, "SearchKey");
        }

        var digits = PhoneNumberNormalizer.ExtractDigits(term);
        if (digits.Length >= 4)
        {
            var reversedPrefix = PhoneNumberNormalizer.ReverseDigits(digits);
            var phonePersonIds = await dbContext
                .PhoneNumbers.AsNoTracking()
                .Where(pn =>
                    pn.Number.Contains(digits) || pn.NumberReversed.StartsWith(reversedPrefix)
                )
                .Select(static pn => pn.PersonId)
                .Distinct()
                .Take(limit)
                .ToListAsync(cancellationToken);

            if (phonePersonIds.Count > 0)
            {
                var matched = await baseQuery
                    .Where(p => phonePersonIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);
                RecordMatches(ranked, matched, priority: 2, "Phone");
            }
        }
    }

    private async Task AddTrigramNameMatchesAsync(
        IQueryable<Person> baseQuery,
        string term,
        int limit,
        Dictionary<PersonId, (Person Person, int Priority, string Reason)> ranked,
        CancellationToken cancellationToken
    )
    {
        var tokens = term.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var pattern = $"%{term}%";
        var firstPattern = tokens.Length >= 2 ? $"%{tokens[0]}%" : pattern;
        var lastPattern = tokens.Length >= 2 ? $"%{tokens[^1]}%" : pattern;

        var directQuery =
            tokens.Length >= 2
                ? baseQuery.Where(p =>
                    (
                        EF.Functions.ILike(p.FirstName, firstPattern)
                        || EF.Functions.ILike(p.NickName, firstPattern)
                    ) && EF.Functions.ILike(p.LastName, lastPattern)
                )
                : baseQuery.Where(p =>
                    EF.Functions.ILike(p.FirstName, pattern)
                    || EF.Functions.ILike(p.NickName, pattern)
                    || EF.Functions.ILike(p.LastName, pattern)
                    || (p.Email != null && EF.Functions.ILike(p.Email, pattern))
                    || EF.Functions.TrigramsSimilarity(p.LastName, term) > 0.35
                    || EF.Functions.TrigramsSimilarity(p.FirstName, term) > 0.35
                    || EF.Functions.TrigramsSimilarity(p.NickName, term) > 0.35
                );

        var matched = await directQuery.Take(limit).ToListAsync(cancellationToken);
        RecordMatches(ranked, matched, priority: 3, "Name/Trigram");

        var prevPattern =
            tokens.Length >= 2 ? tokens[^1].ToUpperInvariant() : term.ToUpperInvariant();
        var prevIds = await dbContext
            .PersonPreviousNames.AsNoTracking()
            .Where(pn => pn.NormalizedLastName == prevPattern)
            .Select(static pn => pn.PersonId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (prevIds.Count > 0)
        {
            var prevMatched = await baseQuery
                .Where(p => prevIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            RecordMatches(ranked, prevMatched, priority: 4, "PreviousName");
        }
    }

    private async Task AddPhoneticAndNicknameMatchesAsync(
        IQueryable<Person> baseQuery,
        string term,
        int limit,
        Dictionary<PersonId, (Person Person, int Priority, string Reason)> ranked,
        CancellationToken cancellationToken
    )
    {
        var firstToken = term.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var upperFirst = firstToken.ToUpperInvariant();

        var nickVariants = await dbContext
            .MetaNickNameLookups.AsNoTracking()
            .Where(n => n.NormalizedFirstName == upperFirst || n.NormalizedNickName == upperFirst)
            .Select(static n => new { n.FirstName, n.NickName })
            .ToListAsync(cancellationToken);

        var expandedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in nickVariants)
        {
            _ = expandedNames.Add(pair.FirstName);
            _ = expandedNames.Add(pair.NickName);
        }

        if (expandedNames.Count > 0)
        {
            var variants = expandedNames.ToArray();
            var nickMatched = await baseQuery
                .Where(p => variants.Contains(p.FirstName) || variants.Contains(p.NickName))
                .Take(limit)
                .ToListAsync(cancellationToken);
            RecordMatches(ranked, nickMatched, priority: 5, "Nickname");
        }

        var encoded = DoubleMetaphoneEncoder.Encode(firstToken);
        if (string.IsNullOrWhiteSpace(encoded.PrimaryKey))
        {
            return;
        }

        var phoneticNames = await dbContext
            .Metaphones.AsNoTracking()
            .Where(m =>
                m.PrimaryKey == encoded.PrimaryKey
                || (encoded.AlternateKey != null && m.AlternateKey == encoded.AlternateKey)
            )
            .Select(static m => m.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (phoneticNames.Count > 0)
        {
            var phoneticMatched = await baseQuery
                .Where(p =>
                    phoneticNames.Contains(p.NormalizedFirstName)
                    || phoneticNames.Contains(p.NormalizedNickName)
                    || phoneticNames.Contains(p.NormalizedLastName)
                )
                .Take(limit)
                .ToListAsync(cancellationToken);
            RecordMatches(ranked, phoneticMatched, priority: 6, "Phonetic");
        }
    }

    private static void RecordMatches(
        Dictionary<PersonId, (Person Person, int Priority, string Reason)> ranked,
        IEnumerable<Person> people,
        int priority,
        string reason
    )
    {
        foreach (var person in people)
        {
            if (!ranked.TryGetValue(person.Id, out var existing) || priority < existing.Priority)
            {
                ranked[person.Id] = (person, priority, reason);
            }
        }
    }
}
