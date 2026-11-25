using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Removes a security role assignment from a user account.</summary>
public sealed record RemoveUserRoleCommand(Guid UserAccountId, Guid SecurityRoleId)
    : ICommand<bool>;
