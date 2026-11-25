using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Removes an unused role from a GroupType.</summary>
public sealed record RemoveGroupTypeRoleCommand(Guid GroupTypeId, Guid RoleId)
    : ICommand<GroupTypeDetailResponse>;
