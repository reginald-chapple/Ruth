using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Public or self-service GroupSimpleRegister registration command.</summary>
public sealed record SimpleRegisterForGroupCommand(
    Guid GroupId,
    Guid PersonId,
    Guid? GroupRoleId = null,
    GroupMemberStatus InitialStatus = GroupMemberStatus.Active,
    int? GuestCount = null,
    string? Note = null
) : ICommand<GroupMemberResponse>;
