using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain;

internal static class GroupsErrors
{
    public static readonly DomainError InvalidText = DomainError.Validation(
        "Groups.InvalidText",
        "Text value contains invalid control characters or exceeds maximum length."
    );

    public static readonly DomainError NameRequired = DomainError.Validation(
        "Groups.NameRequired",
        "Name is required."
    );

    public static readonly DomainError GroupTypeNotFound = DomainError.NotFound(
        "Groups.GroupType.NotFound",
        "GroupType was not found."
    );

    public static readonly DomainError GroupTypeRoleNotFound = DomainError.NotFound(
        "Groups.GroupTypeRole.NotFound",
        "GroupTypeRole was not found."
    );

    public static readonly DomainError DuplicateRoleName = DomainError.Conflict(
        "Groups.GroupTypeRole.DuplicateName",
        "A role with the same name already exists on this GroupType."
    );

    public static readonly DomainError InvalidRoleCounts = DomainError.Validation(
        "Groups.GroupTypeRole.InvalidCounts",
        "Role MinCount and MaxCount must be non-negative and MinCount cannot exceed MaxCount."
    );

    public static readonly DomainError RoleInUse = DomainError.Conflict(
        "Groups.GroupTypeRole.InUse",
        "Cannot remove a role that is still assigned to group members or set as the default role."
    );

    public static readonly DomainError GroupNotFound = DomainError.NotFound(
        "Groups.Group.NotFound",
        "Group was not found."
    );

    public static readonly DomainError ParentGroupCycle = DomainError.Validation(
        "Groups.Group.ParentCycle",
        "Assigning this parent group would create a circular hierarchy."
    );

    public static readonly DomainError ParentGroupChildTypeNotAllowed = DomainError.Validation(
        "Groups.Group.ChildTypeNotAllowed",
        "The parent group's GroupType does not allow child groups of this GroupType."
    );

    public static readonly DomainError CampusRequired = DomainError.Validation(
        "Groups.Group.CampusRequired",
        "This GroupType requires a Campus to be assigned."
    );

    public static readonly DomainError InvalidCapacity = DomainError.Validation(
        "Groups.Group.InvalidCapacity",
        "GroupCapacity cannot be negative."
    );

    public static readonly DomainError MultipleLocationsNotAllowed = DomainError.Validation(
        "Groups.GroupLocation.MultipleNotAllowed",
        "This GroupType does not allow multiple locations on a group."
    );

    public static readonly DomainError GroupLocationNotFound = DomainError.NotFound(
        "Groups.GroupLocation.NotFound",
        "GroupLocation was not found."
    );

    public static readonly DomainError InvalidGeoPoint = DomainError.Validation(
        "Groups.Geo.InvalidPoint",
        "Latitude must be between -90 and 90 and Longitude between -180 and 180."
    );

    public static readonly DomainError InvalidGeoPolygon = DomainError.Validation(
        "Groups.Geo.InvalidPolygon",
        "Polygon geofence must form a valid closed ring with at least 3 distinct points."
    );

    public static readonly DomainError InvalidSearchRadius = DomainError.Validation(
        "Groups.Geo.InvalidRadius",
        "Search radius must be greater than zero."
    );

    public static readonly DomainError DemographicTypeNotFound = DomainError.NotFound(
        "Groups.DemographicType.NotFound",
        "GroupDemographicType was not found."
    );

    public static readonly DomainError MemberNotFound = DomainError.NotFound(
        "Groups.Member.NotFound",
        "GroupMember was not found."
    );

    public static readonly DomainError RoleMismatch = DomainError.Validation(
        "Groups.Member.RoleMismatch",
        "The specified role does not belong to the group's GroupType."
    );

    public static readonly DomainError DefaultRoleRequired = DomainError.Validation(
        "Groups.Member.DefaultRoleRequired",
        "No role was provided and the GroupType has no default role configured."
    );

    public static readonly DomainError DuplicateMemberRole = DomainError.Conflict(
        "Groups.Member.DuplicateRole",
        "This person already belongs to this group in the specified role."
    );

    public static readonly DomainError RoleMaxCountExceeded = DomainError.Conflict(
        "Groups.Member.RoleMaxCountExceeded",
        "Active member count for this role has reached its maximum limit."
    );

    public static readonly DomainError GroupHardCapacityExceeded = DomainError.Conflict(
        "Groups.Member.HardCapacityExceeded",
        "Adding or activating this member would exceed the group's hard capacity."
    );

    public static readonly DomainError RequirementsNotMetForAdd = DomainError.Failure(
        "Groups.Member.RequirementsNotMet",
        "Person does not meet one or more prerequisites required before joining this group."
    );

    public static readonly DomainError GroupNotOpenForRegistration = DomainError.Failure(
        "Groups.Register.NotOpen",
        "This group is inactive, archived, or not open for public registration."
    );

    public static readonly DomainError InvalidGuestCount = DomainError.Validation(
        "Groups.Member.InvalidGuestCount",
        "Guest count cannot be negative."
    );

    public static readonly DomainError RequirementTypeNotFound = DomainError.NotFound(
        "Groups.RequirementType.NotFound",
        "GroupRequirementType was not found."
    );

    public static readonly DomainError RequirementNotFound = DomainError.NotFound(
        "Groups.Requirement.NotFound",
        "GroupRequirement was not found."
    );

    public static readonly DomainError RequirementInvalidScope = DomainError.Validation(
        "Groups.Requirement.InvalidScope",
        "A GroupRequirement must target either a GroupId or a GroupTypeId, but not both."
    );

    public static readonly DomainError RequirementOverrideNotAllowed = DomainError.Forbidden(
        "Groups.Requirement.OverrideNotAllowed",
        "Leader overrides are not enabled on this GroupRequirement."
    );

    public static readonly DomainError SyncRuleNotFound = DomainError.NotFound(
        "Groups.Sync.NotFound",
        "GroupSync rule was not found."
    );

    public static readonly DomainError InvalidSyncSource = DomainError.Validation(
        "Groups.Sync.InvalidSource",
        "GroupSync rule requires a valid source reference or filter value."
    );

    public static readonly DomainError InvalidDateRange = DomainError.Validation(
        "Groups.Schedule.InvalidDateRange",
        "EndDate cannot be earlier than StartDate."
    );

    public static readonly DomainError InvalidScheduleCapacities = DomainError.Validation(
        "Groups.Schedule.InvalidCapacities",
        "Schedule capacities must satisfy 0 <= Minimum <= Desired <= Maximum."
    );

    public static readonly DomainError SchedulingDisabled = DomainError.Failure(
        "Groups.Schedule.Disabled",
        "Scheduling is disabled for this group or its GroupType."
    );

    public static readonly DomainError VolunteerConflict = DomainError.Conflict(
        "Groups.Schedule.Conflict",
        "Volunteer cannot be scheduled due to an exclusion, requirement gap, capacity limit, or double-booking conflict."
    );

    public static readonly DomainError ScheduleTemplateNotFound = DomainError.NotFound(
        "Groups.ScheduleTemplate.NotFound",
        "GroupMemberScheduleTemplate was not found."
    );

    public static readonly DomainError AssignmentNotFound = DomainError.NotFound(
        "Groups.Assignment.NotFound",
        "GroupMemberAssignment was not found."
    );

    public static readonly DomainError ExclusionNotFound = DomainError.NotFound(
        "Groups.Exclusion.NotFound",
        "Schedule exclusion was not found."
    );

    public static readonly DomainError RsvpNotFound = DomainError.NotFound(
        "Groups.Rsvp.NotFound",
        "Scheduled volunteer RSVP record was not found."
    );

    public static readonly DomainError AttendanceNotEnabled = DomainError.Failure(
        "Groups.Attendance.NotEnabled",
        "This group's GroupType does not support taking attendance."
    );

    public static readonly DomainError AttendanceLocationRequired = DomainError.Validation(
        "Groups.Attendance.LocationRequired",
        "This GroupType requires a Location when recording attendance."
    );

    public static readonly DomainError AttendanceScheduleRequired = DomainError.Validation(
        "Groups.Attendance.ScheduleRequired",
        "This GroupType requires a Schedule when recording attendance."
    );

    public static readonly DomainError AttendanceOccurrenceNotFound = DomainError.NotFound(
        "Groups.Attendance.OccurrenceNotFound",
        "AttendanceOccurrence was not found."
    );

    public static readonly DomainError InvalidAnonymousCount = DomainError.Validation(
        "Groups.Attendance.InvalidAnonymousCount",
        "Anonymous attendance count cannot be negative."
    );
}
