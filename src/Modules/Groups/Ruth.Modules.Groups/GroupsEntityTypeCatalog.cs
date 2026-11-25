using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Domain.Sync;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Groups;

internal static class GroupsEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            GroupsSystemGuids.GroupType,
            GroupType.EntityTypeKeyName,
            nameof(GroupType),
            "Group Type",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            GroupsSystemGuids.GroupTypeRole,
            "Ruth.Groups.GroupTypeRole",
            nameof(GroupTypeRole),
            "Group Type Role",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.Group,
            Group.EntityTypeKeyName,
            nameof(Group),
            "Group",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            GroupsSystemGuids.GroupLocation,
            "Ruth.Groups.GroupLocation",
            nameof(GroupLocation),
            "Group Location",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.GroupMember,
            GroupMember.EntityTypeKeyName,
            nameof(GroupMember),
            "Group Member",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            GroupsSystemGuids.GroupRequirementType,
            GroupRequirementType.EntityTypeKeyName,
            nameof(GroupRequirementType),
            "Group Requirement Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.GroupRequirement,
            GroupRequirement.EntityTypeKeyName,
            nameof(GroupRequirement),
            "Group Requirement",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.GroupMemberRequirement,
            GroupMemberRequirement.EntityTypeKeyName,
            nameof(GroupMemberRequirement),
            "Group Member Requirement",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.GroupSyncRule,
            GroupSyncRule.EntityTypeKeyName,
            nameof(GroupSyncRule),
            "Group Sync Rule",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.AttendanceOccurrence,
            AttendanceOccurrence.EntityTypeKeyName,
            nameof(AttendanceOccurrence),
            "Attendance Occurrence",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            GroupsSystemGuids.Attendance,
            "Ruth.Groups.Attendance",
            nameof(Attendance),
            "Attendance",
            isSecured: false,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Groups",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
