using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Assigns a security role to a user account.</summary>
public sealed record AssignUserRoleCommand(Guid UserAccountId, Guid SecurityRoleId)
    : ICommand<bool>;
