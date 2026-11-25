using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Households;

internal sealed class SetHouseholdLocationCommandHandler(CrmDbContext dbContext)
    : ICommandHandler<SetHouseholdLocationCommand, HouseholdResponse>
{
    public async Task<Result<HouseholdResponse>> HandleAsync(
        SetHouseholdLocationCommand command,
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

        var result = household.SetLocation(
            command.LocationId,
            command.LocationType,
            command.IsMailingLocation,
            command.IsMappedLocation,
            command.AddressSummary,
            command.MoveExistingToPrevious
        );
        if (result.IsFailure)
        {
            return Result.Failure<HouseholdResponse>(result.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            await HouseholdMapper.MapAsync(dbContext, household, cancellationToken)
        );
    }
}
