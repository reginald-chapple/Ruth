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

internal sealed class CreateHouseholdCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<CreateHouseholdCommand, HouseholdResponse>
{
    public async Task<Result<HouseholdResponse>> HandleAsync(
        CreateHouseholdCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Household.Create(
            command.Name,
            command.CampusId,
            command.CombineGivingByDefault
        );
        if (created.IsFailure)
        {
            return Result.Failure<HouseholdResponse>(created.Errors);
        }

        var household = created.Value;
        var membersResult = await AttachInitialMembersAsync(
            household,
            command.Members,
            cancellationToken
        );
        if (membersResult.IsFailure)
        {
            return Result.Failure<HouseholdResponse>(membersResult.Errors);
        }

        _ = dbContext.Households.Add(household);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await SyncDerivedStateAndGraphAsync(household, membersResult.Value, cancellationToken);

        return Result.Success(
            await HouseholdMapper.MapAsync(dbContext, household, cancellationToken)
        );
    }

    private async Task<Result<Dictionary<PersonId, string>>> AttachInitialMembersAsync(
        Household household,
        IReadOnlyList<HouseholdMemberInputDto>? members,
        CancellationToken cancellationToken
    )
    {
        var memberNames = new Dictionary<PersonId, string>();
        if (members is null || members.Count == 0)
        {
            return Result.Success(memberNames);
        }

        var requestedIds = members
            .Select(static m => PersonId.From(m.PersonId))
            .Distinct()
            .ToArray();
        var people = await dbContext
            .People.Where(p => requestedIds.Contains(p.Id))
            .ToDictionaryAsync(static p => p.Id, cancellationToken);

        foreach (var input in members)
        {
            var pid = PersonId.From(input.PersonId);
            if (!people.TryGetValue(pid, out var person))
            {
                return CrmErrors.PersonNotFound;
            }

            var added = household.AddOrUpdateMember(
                pid,
                input.Role,
                input.HouseholdOrder,
                input.IsCombinedGivingMember
            );
            if (added.IsFailure)
            {
                return Result.Failure<Dictionary<PersonId, string>>(added.Errors);
            }

            memberNames[pid] = person.FullName;
        }

        return Result.Success(memberNames);
    }

    private async Task SyncDerivedStateAndGraphAsync(
        Household household,
        Dictionary<PersonId, string> memberNames,
        CancellationToken cancellationToken
    )
    {
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

        if (household.Members.Count > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        await graphService.UpsertHouseholdVertexAsync(
            household.Id.Value,
            household.Name,
            cancellationToken
        );
        foreach (var member in household.Members)
        {
            await graphService.UpsertHouseholdMembershipEdgeAsync(
                member.PersonId.Value,
                memberNames[member.PersonId],
                household.Id.Value,
                household.Name,
                member.Role.ToString(),
                cancellationToken
            );
        }
    }
}
