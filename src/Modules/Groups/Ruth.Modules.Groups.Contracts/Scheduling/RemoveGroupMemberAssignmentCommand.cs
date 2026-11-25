using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Removes a standing GroupMemberAssignment.</summary>
public sealed record RemoveGroupMemberAssignmentCommand(Guid AssignmentId) : ICommand<bool>;
