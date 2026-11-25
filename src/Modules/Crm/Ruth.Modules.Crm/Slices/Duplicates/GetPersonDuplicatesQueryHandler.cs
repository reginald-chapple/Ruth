using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class GetPersonDuplicatesQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonDuplicatesQuery, IReadOnlyList<PersonDuplicateResponse>>
{
    public async Task<Result<IReadOnlyList<PersonDuplicateResponse>>> HandleAsync(
        GetPersonDuplicatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var rows = await QueryDuplicateRowsAsync(query, cancellationToken);
        if (rows.Count == 0)
        {
            return Result.Success<IReadOnlyList<PersonDuplicateResponse>>([]);
        }

        var personIds = rows.SelectMany(static r => new[] { r.Person1Id, r.Person2Id })
            .Distinct()
            .ToArray();
        var people = await dbContext
            .People.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        return Result.Success<IReadOnlyList<PersonDuplicateResponse>>(MapRows(rows, people));
    }

    private Task<List<PersonDuplicate>> QueryDuplicateRowsAsync(
        GetPersonDuplicatesQuery query,
        CancellationToken cancellationToken
    )
    {
        var limit = Math.Clamp(query.MaxResults, 1, 200);
        var q = dbContext
            .PersonDuplicates.AsNoTracking()
            .Where(d => d.ConfidenceScore >= query.MinConfidenceScore);

        if (!query.IncludeConfirmedNotDuplicate)
        {
            q = q.Where(static d => !d.IsConfirmedAsNotDuplicate);
        }

        if (!query.IncludeIgnored)
        {
            q = q.Where(static d => !d.IgnoreUntilScoreChanges);
        }

        return q.OrderByDescending(static d => d.ConfidenceScore)
            .ThenByDescending(static d => d.Score)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private static List<PersonDuplicateResponse> MapRows(
        List<PersonDuplicate> rows,
        Dictionary<PersonId, Person> people
    )
    {
        return
        [
            .. rows.Where(r =>
                    people.TryGetValue(r.Person1Id, out var p1)
                    && people.TryGetValue(r.Person2Id, out var p2)
                    && !p1.IsMerged
                    && !p2.IsMerged
                )
                .Select(r =>
                {
                    var p1 = people[r.Person1Id];
                    var p2 = people[r.Person2Id];
                    return new PersonDuplicateResponse(
                        r.Id.Value,
                        p1.Id.Value,
                        p1.FullName,
                        p1.Email,
                        p2.Id.Value,
                        p2.FullName,
                        p2.Email,
                        r.Score,
                        r.Capacity,
                        r.TotalCapacity,
                        r.ConfidenceScore,
                        r.ScoreDetail,
                        r.IsConfirmedAsNotDuplicate,
                        r.IgnoreUntilScoreChanges,
                        r.LastScannedOnUtc
                    );
                }),
        ];
    }
}
