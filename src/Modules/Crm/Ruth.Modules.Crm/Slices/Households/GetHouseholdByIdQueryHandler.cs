using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Households;

internal sealed class GetHouseholdByIdQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetHouseholdByIdQuery, HouseholdResponse>
{
    public async Task<Result<HouseholdResponse>> HandleAsync(
        GetHouseholdByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var householdId = HouseholdId.From(query.HouseholdId);
        var household = await dbContext
            .Households.AsNoTracking()
            .AsSplitQuery()
            .Include(static h => h.Members)
            .Include(static h => h.Locations)
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);

        return household is null
            ? CrmErrors.HouseholdNotFound
            : Result.Success(
                await HouseholdMapper.MapAsync(dbContext, household, cancellationToken)
            );
    }
}
