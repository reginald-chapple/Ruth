using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Households;

internal sealed class RemoveHouseholdMemberCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<RemoveHouseholdMemberCommand, HouseholdResponse>
{
    public async Task<Result<HouseholdResponse>> HandleAsync(
        RemoveHouseholdMemberCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var householdId = HouseholdId.From(command.HouseholdId);
        var personId = PersonId.From(command.PersonId);

        var household = await dbContext
            .Households.Include(static h => h.Members)
            .Include(static h => h.Locations)
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);
        if (household is null)
        {
            return CrmErrors.HouseholdNotFound;
        }

        var removed = household.RemoveMember(personId);
        if (removed.IsFailure)
        {
            return Result.Failure<HouseholdResponse>(removed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await PersonHouseholdSyncHelper.SyncPersonDerivedFieldsAsync(
            dbContext,
            personId,
            DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await graphService.RemoveHouseholdMembershipEdgeAsync(
            personId.Value,
            householdId.Value,
            cancellationToken
        );

        return Result.Success(
            await HouseholdMapper.MapAsync(dbContext, household, cancellationToken)
        );
    }
}
