using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Scheduling;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal static class ServingScheduleGridBuilder
{
    public static List<ServingScheduleSlotResponse> BuildSlots(
        Group group,
        List<AttendanceOccurrence> occurrences,
        List<GroupLocationScheduleConfig> configs,
        List<GroupScheduleExclusion> groupExclusions,
        List<PersonScheduleExclusion> personExclusions
    )
    {
        var locByLocationId = group.Locations.ToDictionary(static l => l.LocationId);
        var configLookup = BuildConfigLookup(group, configs);

        var slots = new List<ServingScheduleSlotResponse>(occurrences.Count);
        foreach (var occ in occurrences)
        {
            slots.Add(
                BuildSingleSlot(
                    group,
                    occ,
                    locByLocationId,
                    configLookup,
                    groupExclusions,
                    personExclusions
                )
            );
        }

        return slots;
    }

    public static List<ScheduleCandidateResponse> BuildCandidates(
        Group group,
        GroupType groupType,
        List<GroupMember> members,
        HashSet<GroupMemberId> unmetMemberIds,
        Dictionary<GroupMemberScheduleTemplateId, GroupMemberScheduleTemplate> templates,
        List<PersonScheduleExclusion> personExclusions,
        List<GroupScheduleExclusion> groupExclusions,
        DateOnly referenceDate
    )
    {
        var roleNames = groupType.Roles.ToDictionary(static r => r.Id, static r => r.Name);
        var dateExcluded = groupExclusions.Exists(e => e.Covers(referenceDate));
        var enabled = groupType.IsSchedulingEnabled && !group.DisableScheduling;

        var candidates = new List<ScheduleCandidateResponse>(members.Count);
        foreach (var member in members)
        {
            var hasBlackout = personExclusions.Exists(e =>
                e.PersonId == member.PersonId && e.Covers(referenceDate, group.Id)
            );
            var hasGap = group.SchedulingMustMeetRequirements && unmetMemberIds.Contains(member.Id);
            var matchesTemplate =
                !member.ScheduleTemplateId.HasValue
                || !templates.TryGetValue(member.ScheduleTemplateId.Value, out var tpl)
                || tpl.MatchesDate(member.ScheduleStartDate, referenceDate);

            candidates.Add(
                new ScheduleCandidateResponse(
                    member.Id.Value,
                    member.PersonId,
                    roleNames.GetValueOrDefault(member.GroupRoleId, "Member"),
                    hasBlackout,
                    hasGap,
                    HasDoubleBookingOnDate: false,
                    matchesTemplate,
                    ServingScheduleEvaluator.IsCandidateEligible(
                        enabled,
                        dateExcluded,
                        hasBlackout,
                        hasGap,
                        hasDoubleBooking: false
                    )
                )
            );
        }

        return candidates;
    }

    private static Dictionary<(Guid, Guid), GroupLocationScheduleConfig> BuildConfigLookup(
        Group group,
        List<GroupLocationScheduleConfig> configs
    )
    {
        var lookup = new Dictionary<(Guid, Guid), GroupLocationScheduleConfig>();
        foreach (var cfg in configs)
        {
            var loc = group.Locations.FirstOrDefault(l => l.Id == cfg.GroupLocationId);
            if (loc is not null)
            {
                lookup[(loc.LocationId, cfg.ScheduleId)] = cfg;
            }
        }

        return lookup;
    }

    private static ServingScheduleSlotResponse BuildSingleSlot(
        Group group,
        AttendanceOccurrence occ,
        Dictionary<Guid, GroupLocation> locByLocationId,
        Dictionary<(Guid, Guid), GroupLocationScheduleConfig> configLookup,
        List<GroupScheduleExclusion> groupExclusions,
        List<PersonScheduleExclusion> personExclusions
    )
    {
        var locationName =
            occ.LocationId.HasValue
            && locByLocationId.TryGetValue(occ.LocationId.Value, out var loc)
                ? loc.LocationName
                : "Unassigned Location";

        GroupLocationScheduleConfig? cfg = null;
        if (occ.LocationId.HasValue && occ.ScheduleId.HasValue)
        {
            _ = configLookup.TryGetValue((occ.LocationId.Value, occ.ScheduleId.Value), out cfg);
        }

        var scheduled = ProjectVolunteers(group, occ, locationName, personExclusions);
        var confirmed = scheduled.Count(static v =>
            v.ScheduledToAttend && v.Rsvp == RsvpStatus.Yes
        );
        var declined = scheduled.Count(static v => v.Rsvp == RsvpStatus.No);
        var pending = scheduled.Count(static v => v.Rsvp is RsvpStatus.Unknown or RsvpStatus.Maybe);

        return new ServingScheduleSlotResponse(
            group.Id.Value,
            occ.LocationId,
            locationName,
            occ.ScheduleId,
            occ.OccurrenceDate,
            groupExclusions.Exists(e => e.Covers(occ.OccurrenceDate)),
            cfg?.MinimumCapacity,
            cfg?.DesiredCapacity,
            cfg?.MaximumCapacity,
            confirmed,
            pending,
            declined,
            ServingScheduleEvaluator.EvaluateCapacityStatus(
                confirmed + pending,
                cfg?.MinimumCapacity,
                cfg?.DesiredCapacity,
                cfg?.MaximumCapacity
            ),
            scheduled
        );
    }

    private static List<ScheduledVolunteerResponse> ProjectVolunteers(
        Group group,
        AttendanceOccurrence occ,
        string locationName,
        List<PersonScheduleExclusion> personExclusions
    )
    {
        return
        [
            .. occ
                .Attendances.Where(static a => a.RequestedToAttend == true)
                .Select(a =>
                    ScheduleVolunteerService.ToScheduledResponse(
                        a,
                        occ,
                        group.Name,
                        locationName,
                        hasBlackout: a.PersonId.HasValue
                            && personExclusions.Exists(e =>
                                e.PersonId == a.PersonId.Value
                                && e.Covers(occ.OccurrenceDate, group.Id)
                            ),
                        hasDoubleBooking: false,
                        meetsRequirements: true
                    )
                ),
        ];
    }
}
