using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;

namespace Ruth.Modules.Crm.Slices.Households;

internal static class HouseholdMapper
{
    public static async Task<HouseholdResponse> MapAsync(
        CrmDbContext dbContext,
        Household household,
        CancellationToken cancellationToken
    )
    {
        var memberIds = household.Members.Select(static m => m.PersonId).Distinct().ToArray();
        var people =
            memberIds.Length == 0
                ? []
                : await dbContext
                    .People.AsNoTracking()
                    .Where(p => memberIds.Contains(p.Id))
                    .ToDictionaryAsync(static p => p.Id, cancellationToken);

        return MapWithPeople(household, people);
    }

    public static HouseholdResponse MapWithPeople(
        Household household,
        IReadOnlyDictionary<PersonId, Person> peopleById
    )
    {
        var salutation = HouseholdDomainService.ComputeSalutation(
            household,
            peopleById,
            includeChildren: false
        );
        var salutationFull = HouseholdDomainService.ComputeSalutation(
            household,
            peopleById,
            includeChildren: true
        );
        var givingLeader = HouseholdDomainService.ResolveGivingLeaderId(household, peopleById);

        var members = household
            .Members.OrderBy(static m => (int)m.Role)
            .ThenBy(static m => m.HouseholdOrder)
            .Select(m =>
            {
                var fullName = peopleById.TryGetValue(m.PersonId, out var person)
                    ? person.FullName
                    : m.PersonId.ToString();
                return new HouseholdMemberResponse(
                    m.Id.Value,
                    m.PersonId.Value,
                    fullName,
                    m.Role,
                    m.HouseholdOrder,
                    m.IsCombinedGivingMember
                );
            })
            .ToList();

        var locations = household
            .Locations.OrderBy(static l => (int)l.LocationType)
            .Select(static l => new HouseholdLocationResponse(
                l.Id.Value,
                l.LocationId,
                l.LocationType,
                l.IsMailingLocation,
                l.IsMappedLocation,
                l.AddressSummary
            ))
            .ToList();

        return new HouseholdResponse(
            household.Id.Value,
            household.Name,
            household.CampusId,
            household.IsActive,
            household.IsArchived,
            household.CombineGivingByDefault,
            salutation,
            salutationFull,
            givingLeader?.Value,
            members,
            locations,
            household.CreatedOnUtc,
            household.ModifiedOnUtc
        );
    }
}
