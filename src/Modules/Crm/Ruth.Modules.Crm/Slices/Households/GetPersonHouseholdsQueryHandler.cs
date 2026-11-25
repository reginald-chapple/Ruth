using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Households;

internal sealed class GetPersonHouseholdsQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonHouseholdsQuery, IReadOnlyList<HouseholdResponse>>
{
    public async Task<Result<IReadOnlyList<HouseholdResponse>>> HandleAsync(
        GetPersonHouseholdsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var householdIds = await dbContext
            .HouseholdMembers.AsNoTracking()
            .Where(m => m.PersonId == personId)
            .OrderBy(static m => m.HouseholdOrder)
            .Select(static m => m.HouseholdId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (householdIds.Count == 0)
        {
            return Result.Success<IReadOnlyList<HouseholdResponse>>([]);
        }

        var households = await dbContext
            .Households.AsNoTracking()
            .AsSplitQuery()
            .Include(static h => h.Members)
            .Include(static h => h.Locations)
            .Where(h => householdIds.Contains(h.Id) && !h.IsArchived)
            .ToListAsync(cancellationToken);

        var allPersonIds = households
            .SelectMany(static h => h.Members.Select(static m => m.PersonId))
            .Distinct()
            .ToArray();

        var peopleById = await dbContext
            .People.AsNoTracking()
            .Where(p => allPersonIds.Contains(p.Id))
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        var ordered = households
            .OrderBy(h =>
                h.Members.FirstOrDefault(m => m.PersonId == personId)?.HouseholdOrder
                ?? int.MaxValue
            )
            .Select(h => HouseholdMapper.MapWithPeople(h, peopleById))
            .ToList();

        return Result.Success<IReadOnlyList<HouseholdResponse>>(ordered);
    }
}
