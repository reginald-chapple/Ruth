using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;

namespace Ruth.Modules.Crm.Slices.Households;

internal static class PersonHouseholdSyncHelper
{
    public static async Task SyncPersonDerivedFieldsAsync(
        CrmDbContext dbContext,
        PersonId personId,
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        var person = await dbContext.People.FirstOrDefaultAsync(
            p => p.Id == personId,
            cancellationToken
        );
        if (person is null)
        {
            return;
        }

        var memberships = await (
            from m in dbContext.HouseholdMembers
            join h in dbContext.Households on m.HouseholdId equals h.Id
            where m.PersonId == personId && !h.IsArchived
            orderby m.HouseholdOrder, h.Id
            select new
            {
                h.Id,
                h.CampusId,
                m.Role,
                m.IsCombinedGivingMember,
            }
        ).ToListAsync(cancellationToken);

        var primary = memberships.FirstOrDefault();
        var giving = memberships.FirstOrDefault(static m => m.IsCombinedGivingMember);
        var hasAdult = memberships.Exists(static m => m.Role == HouseholdRole.Adult);
        var hasChild = memberships.Exists(static m => m.Role == HouseholdRole.Child);

        person.SyncHouseholdDerivedState(
            primary?.Id.Value,
            primary?.CampusId,
            giving?.Id.Value,
            hasAdult,
            hasChild,
            today
        );
    }
}
