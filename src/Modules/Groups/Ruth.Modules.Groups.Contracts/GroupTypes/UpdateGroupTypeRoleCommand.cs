using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Updates an existing role within a GroupType.</summary>
public sealed record UpdateGroupTypeRoleCommand(
    Guid GroupTypeId,
    Guid RoleId,
    GroupTypeRoleInputDto Role
) : ICommand<GroupTypeDetailResponse>;
