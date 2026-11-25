using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Removes an attached GroupLocation from a Group.</summary>
public sealed record RemoveGroupLocationCommand(Guid GroupId, Guid GroupLocationId)
    : ICommand<GroupDetailResponse>;
