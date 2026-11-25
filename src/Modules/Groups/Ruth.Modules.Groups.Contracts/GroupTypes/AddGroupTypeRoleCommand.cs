using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Adds a new role to an existing GroupType.</summary>
public sealed record AddGroupTypeRoleCommand(
    Guid GroupTypeId,
    GroupTypeRoleInputDto Role,
    bool SetAsDefault = false
) : ICommand<GroupTypeDetailResponse>;
