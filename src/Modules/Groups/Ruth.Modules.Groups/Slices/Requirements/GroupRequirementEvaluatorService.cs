using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class GroupRequirementEvaluatorService(
    GroupsDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
)
{
    public async Task<bool> CheckMustMeetRequirementsForAddAsync(
        Group group,
        Guid personId,
        GroupTypeRoleId roleId,
        CancellationToken cancellationToken
    )
    {
        var requirements = await LoadApplicableRequirementsAsync(group, cancellationToken);
        var gating = requirements.Where(static r => r.MustMeetRequirementToAddMember).ToList();
        if (gating.Count == 0)
        {
            return true;
        }

        GroupRequirementTypeId[] typeIds = [.. gating.Select(static r => r.GroupRequirementTypeId)];
        var types = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        var personProfile = await TryLoadPersonAsync(personId, cancellationToken);
        var isAdult = personProfile is null
            ? (bool?)null
            : personProfile.AgeClassification != AgeClassification.Child;

        foreach (var req in gating)
        {
            if (
                !req.AppliesTo(roleId, isAdult)
                || !types.TryGetValue(req.GroupRequirementTypeId, out var type)
                || type.CheckType == RequirementCheckType.Manual
            )
            {
                continue;
            }

            var state = await EvaluateSingleAsync(type, personId, personProfile, cancellationToken);
            if (state is MeetsGroupRequirement.NotMet or MeetsGroupRequirement.Error)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<GroupMemberRequirementResponse>> EvaluateAndPersistMemberAsync(
        GroupMember member,
        Group group,
        CancellationToken cancellationToken
    )
    {
        var requirements = await LoadApplicableRequirementsAsync(group, cancellationToken);
        if (requirements.Count == 0)
        {
            return [];
        }

        GroupRequirementTypeId[] typeIds =
        [
            .. requirements.Select(static r => r.GroupRequirementTypeId),
        ];
        var types = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        var existingRows = await dbContext
            .GroupMemberRequirements.Where(mr => mr.GroupMemberId == member.Id)
            .ToDictionaryAsync(static mr => mr.GroupRequirementId, cancellationToken);

        var personProfile = await TryLoadPersonAsync(member.PersonId, cancellationToken);
        var isAdult = personProfile is null
            ? (bool?)null
            : personProfile.AgeClassification != AgeClassification.Child;
        var nowUtc = clock.UtcNow;
        var responses = new List<GroupMemberRequirementResponse>(requirements.Count);
        foreach (var req in requirements)
        {
            if (!types.TryGetValue(req.GroupRequirementTypeId, out var type))
            {
                continue;
            }

            var response = await EvaluateSingleMemberRequirementAsync(
                member,
                group,
                req,
                type,
                existingRows,
                personProfile,
                isAdult,
                nowUtc,
                cancellationToken
            );
            responses.Add(response);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return responses;
    }

    private async Task<GroupMemberRequirementResponse> EvaluateSingleMemberRequirementAsync(
        GroupMember member,
        Group group,
        GroupRequirement req,
        GroupRequirementType type,
        Dictionary<GroupRequirementId, GroupMemberRequirement> existingRows,
        PersonDetailResponse? personProfile,
        bool? isAdult,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var applies = req.AppliesTo(member.GroupRoleId, isAdult);
        var dueDate = GroupRequirementPolicy.ComputeDueDate(
            type,
            req,
            member.DateTimeAddedUtc,
            group.AttributeValues,
            nowUtc
        );
        var state =
            !applies ? MeetsGroupRequirement.NotApplicable
            : type.CheckType == RequirementCheckType.Manual ? MeetsGroupRequirement.NotMet
            : await EvaluateSingleAsync(type, member.PersonId, personProfile, cancellationToken);

        if (!existingRows.TryGetValue(req.Id, out var row))
        {
            row = GroupMemberRequirement.Create(member.Id, req.Id, dueDate);
            _ = dbContext.GroupMemberRequirements.Add(row);
            existingRows[req.Id] = row;
        }

        row.ApplyAutomatedEvaluation(state, dueDate, nowUtc);
        return ToMemberRequirementResponse(row, member, type);
    }

    public async Task<bool> MemberMeetsAllRequirementsForSchedulingAsync(
        GroupMember member,
        Group group,
        CancellationToken cancellationToken
    )
    {
        var evaluated = await EvaluateAndPersistMemberAsync(member, group, cancellationToken);
        return evaluated.All(static r =>
            r.State
                is MeetsGroupRequirement.Meets
                    or MeetsGroupRequirement.MeetsWithWarning
                    or MeetsGroupRequirement.NotApplicable
        );
    }

    public static GroupMemberRequirementResponse ToMemberRequirementResponse(
        GroupMemberRequirement row,
        GroupMember member,
        GroupRequirementType type
    )
    {
        return new GroupMemberRequirementResponse(
            row.Id.Value,
            row.GroupMemberId.Value,
            member.GroupId.Value,
            member.PersonId,
            row.GroupRequirementId.Value,
            type.Id.Value,
            type.Name,
            type.CheckType,
            row.State,
            type.ResolveLabel(row.State),
            row.RequirementMetOnUtc,
            row.RequirementFailOnUtc,
            row.RequirementWarningOnUtc,
            row.LastCheckedOnUtc,
            row.DueDate,
            row.WasManuallyCompleted,
            row.ManuallyCompletedByPersonId,
            row.ManuallyCompletedOnUtc,
            row.WasOverridden,
            row.OverriddenByPersonId,
            row.OverriddenOnUtc,
            row.OverrideNote
        );
    }

    private async Task<List<GroupRequirement>> LoadApplicableRequirementsAsync(
        Group group,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .GroupRequirements.AsNoTracking()
            .Where(r => r.GroupId == group.Id || r.GroupTypeId == group.GroupTypeId)
            .ToListAsync(cancellationToken);
    }

    private async Task<PersonDetailResponse?> TryLoadPersonAsync(
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var res = await queryDispatcher.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
            new GetPersonByIdQuery(personId),
            cancellationToken
        );
        return res.IsSuccess ? res.Value : null;
    }

    private async Task<MeetsGroupRequirement> EvaluateSingleAsync(
        GroupRequirementType type,
        Guid personId,
        PersonDetailResponse? personProfile,
        CancellationToken cancellationToken
    )
    {
        return type.CheckType switch
        {
            RequirementCheckType.BackgroundCheckClear => await EvaluateBackgroundCheckAsync(
                type,
                personId,
                cancellationToken
            ),
            RequirementCheckType.PersonAttribute => EvaluatePersonAttribute(type, personProfile),
            RequirementCheckType.GroupMembership => await EvaluatePrerequisiteGroupAsync(
                type,
                personId,
                cancellationToken
            ),
            RequirementCheckType.Manual or _ => MeetsGroupRequirement.NotMet,
        };
    }

    private async Task<MeetsGroupRequirement> EvaluateBackgroundCheckAsync(
        GroupRequirementType type,
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var checksResult = await queryDispatcher.QueryAsync<
            GetPersonBackgroundChecksQuery,
            IReadOnlyList<BackgroundCheckResponse>
        >(new GetPersonBackgroundChecksQuery(personId), cancellationToken);
        if (checksResult.IsFailure)
        {
            return MeetsGroupRequirement.NotMet;
        }

        var clearCheck = checksResult
            .Value.Where(c =>
                c.Status == BackgroundCheckStatus.ReportClear
                && (
                    string.IsNullOrWhiteSpace(type.RuleKey)
                    || string.Equals(
                        c.PackageName,
                        type.RuleKey,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .OrderByDescending(static c => c.RespondedOnUtc ?? c.RequestedOnUtc)
            .FirstOrDefault();

        return GroupRequirementPolicy.EvaluateTimedEvidence(
            type,
            clearCheck?.RespondedOnUtc ?? clearCheck?.RequestedOnUtc,
            clock.UtcNow
        );
    }

    private static MeetsGroupRequirement EvaluatePersonAttribute(
        GroupRequirementType type,
        PersonDetailResponse? personProfile
    )
    {
        return personProfile is null
            || string.IsNullOrWhiteSpace(type.RuleKey)
            || !personProfile.Attributes.TryGetValue(type.RuleKey, out var actual)
                ? MeetsGroupRequirement.NotMet
            : string.IsNullOrWhiteSpace(type.RuleValue)
                ? !string.IsNullOrWhiteSpace(actual) ? MeetsGroupRequirement.Meets
                    : MeetsGroupRequirement.NotMet
            : string.Equals(actual, type.RuleValue, StringComparison.OrdinalIgnoreCase)
                ? MeetsGroupRequirement.Meets
            : MeetsGroupRequirement.NotMet;
    }

    private async Task<MeetsGroupRequirement> EvaluatePrerequisiteGroupAsync(
        GroupRequirementType type,
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        if (!Guid.TryParse(type.RuleKey, out var prereqGuid))
        {
            return MeetsGroupRequirement.Error;
        }

        var prereqGroupId = GroupId.From(prereqGuid);
        var activeMember = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m =>
                m.GroupId == prereqGroupId
                && m.PersonId == personId
                && !m.IsArchived
                && m.Status == GroupMemberStatus.Active
            )
            .OrderByDescending(static m => m.DateTimeAddedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return GroupRequirementPolicy.EvaluateTimedEvidence(
            type,
            activeMember?.DateTimeAddedUtc ?? activeMember?.CreatedOnUtc,
            clock.UtcNow
        );
    }
}
