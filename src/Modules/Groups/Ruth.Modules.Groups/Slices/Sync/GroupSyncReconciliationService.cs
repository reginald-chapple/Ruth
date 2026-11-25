using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Slices.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Sync;

internal sealed class GroupSyncReconciliationService(
    GroupsDbContext dbContext,
    GroupMemberAddService addService,
    IQueryDispatcher queryDispatcher,
    IClock clock
)
{
    public async Task<IReadOnlyList<GroupSyncExecutionResponse>> ReconcileAsync(
        GroupSyncRuleId? ruleId,
        bool forceRun,
        CancellationToken cancellationToken
    )
    {
        var rulesQuery = dbContext.GroupSyncRules.AsQueryable();
        if (ruleId.HasValue)
        {
            rulesQuery = rulesQuery.Where(r => r.Id == ruleId.Value);
        }

        var rules = await rulesQuery.ToListAsync(cancellationToken);
        var nowUtc = clock.UtcNow;
        var results = new List<GroupSyncExecutionResponse>(rules.Count);

        foreach (var rule in rules)
        {
            if (!forceRun && !rule.IsDue(nowUtc))
            {
                continue;
            }

            var execution = await ReconcileSingleRuleAsync(rule, nowUtc, cancellationToken);
            results.Add(execution);
        }

        return results;
    }

    private async Task<GroupSyncExecutionResponse> ReconcileSingleRuleAsync(
        GroupSyncRule rule,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var desiredPeople = await ResolveSourcePeopleAsync(rule, cancellationToken);
        var existingMembers = await dbContext
            .GroupMembers.Where(m =>
                m.GroupId == rule.GroupId && m.GroupRoleId == rule.GroupTypeRoleId && !m.IsArchived
            )
            .ToListAsync(cancellationToken);

        var (added, skipped) = rule.AddMissingMembers
            ? await AddMissingMembersAsync(rule, desiredPeople, existingMembers, cancellationToken)
            : (0, 0);
        var removed = rule.RemoveExtraMembers
            ? ArchiveExtraMembers(existingMembers, desiredPeople, nowUtc)
            : 0;

        rule.RecordExecution(nowUtc, added, removed);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new GroupSyncExecutionResponse(
            rule.Id.Value,
            rule.GroupId.Value,
            desiredPeople.Count,
            added,
            removed,
            skipped,
            nowUtc
        );
    }

    private async Task<(int Added, int Skipped)> AddMissingMembersAsync(
        GroupSyncRule rule,
        HashSet<Guid> desiredPeople,
        List<Domain.Members.GroupMember> existingMembers,
        CancellationToken cancellationToken
    )
    {
        var existingSet = new HashSet<Guid>(existingMembers.Select(static m => m.PersonId));
        var added = 0;
        var skipped = 0;
        foreach (var personId in desiredPeople)
        {
            if (existingSet.Contains(personId))
            {
                continue;
            }

            var addResult = await addService.AddMemberAsync(
                new AddGroupMemberCommand(
                    rule.GroupId.Value,
                    personId,
                    rule.GroupTypeRoleId.Value,
                    rule.TargetMemberStatus
                ),
                cancellationToken
            );
            if (addResult.IsSuccess)
            {
                added++;
            }
            else
            {
                skipped++;
            }
        }

        return (added, skipped);
    }

    private static int ArchiveExtraMembers(
        List<Domain.Members.GroupMember> existingMembers,
        HashSet<Guid> desiredPeople,
        DateTimeOffset nowUtc
    )
    {
        var removed = 0;
        foreach (var member in existingMembers)
        {
            if (!desiredPeople.Contains(member.PersonId))
            {
                member.Archive(archivedByPersonId: null, nowUtc);
                removed++;
            }
        }

        return removed;
    }

    private async Task<HashSet<Guid>> ResolveSourcePeopleAsync(
        GroupSyncRule rule,
        CancellationToken cancellationToken
    )
    {
        return rule.SourceType switch
        {
            GroupSyncSourceType.EntitySet => await ResolveFromEntitySetAsync(
                rule.SourceReferenceId,
                cancellationToken
            ),
            GroupSyncSourceType.CrmConnectionStatus => await ResolveFromCrmStatusAsync(
                rule.SourceFilterValue,
                cancellationToken
            ),
            GroupSyncSourceType.GroupMembers => await ResolveFromGroupMembersAsync(
                rule.SourceReferenceId,
                cancellationToken
            ),
            _ => [],
        };
    }

    private async Task<HashSet<Guid>> ResolveFromEntitySetAsync(
        Guid? entitySetId,
        CancellationToken cancellationToken
    )
    {
        if (!entitySetId.HasValue)
        {
            return [];
        }

        var setsResult = await queryDispatcher.QueryAsync<
            GetEntitySetsQuery,
            IReadOnlyList<EntitySetResponse>
        >(new GetEntitySetsQuery(IncludeExpired: false), cancellationToken);
        if (setsResult.IsFailure)
        {
            return [];
        }

        var set = setsResult.Value.FirstOrDefault(s => s.Id == entitySetId.Value);
        return set is null ? [] : [.. set.Items.Select(static i => i.EntityId)];
    }

    private async Task<HashSet<Guid>> ResolveFromCrmStatusAsync(
        string? filterValue,
        CancellationToken cancellationToken
    )
    {
        if (
            string.IsNullOrWhiteSpace(filterValue)
            || !Enum.TryParse<PersonConnectionStatus>(filterValue, ignoreCase: true, out var status)
        )
        {
            return [];
        }

        var peopleResult = await queryDispatcher.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(
            new SearchPeopleQuery(
                ConnectionStatus: status,
                RecordStatus: PersonRecordStatus.Active,
                MaxResults: 1000
            ),
            cancellationToken
        );

        return peopleResult.IsFailure ? [] : [.. peopleResult.Value.Select(static p => p.Id)];
    }

    private async Task<HashSet<Guid>> ResolveFromGroupMembersAsync(
        Guid? sourceGroupGuid,
        CancellationToken cancellationToken
    )
    {
        if (!sourceGroupGuid.HasValue)
        {
            return [];
        }

        var sourceGroupId = GroupId.From(sourceGroupGuid.Value);
        var personIds = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m =>
                m.GroupId == sourceGroupId && !m.IsArchived && m.Status == GroupMemberStatus.Active
            )
            .Select(static m => m.PersonId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. personIds];
    }
}
