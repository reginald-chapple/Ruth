using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Households;

internal sealed class UpdateHouseholdCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<UpdateHouseholdCommand, HouseholdResponse>
{
    public async Task<Result<HouseholdResponse>> HandleAsync(
        UpdateHouseholdCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var householdId = HouseholdId.From(command.HouseholdId);
        var household = await dbContext
            .Households.Include(static h => h.Members)
            .Include(static h => h.Locations)
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);

        if (household is null)
        {
            return CrmErrors.HouseholdNotFound;
        }

        var updated = household.Update(
            command.Name,
            command.CampusId,
            command.IsActive,
            command.CombineGivingByDefault
        );
        if (updated.IsFailure)
        {
            return Result.Failure<HouseholdResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        foreach (var member in household.Members)
        {
            await PersonHouseholdSyncHelper.SyncPersonDerivedFieldsAsync(
                dbContext,
                member.PersonId,
                today,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await graphService.UpsertHouseholdVertexAsync(
            household.Id.Value,
            household.Name,
            cancellationToken
        );

        return Result.Success(
            await HouseholdMapper.MapAsync(dbContext, household, cancellationToken)
        );
    }
}
