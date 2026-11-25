using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Deletes a non-system security role.</summary>
public sealed record DeleteSecurityRoleCommand(Guid RoleId) : ICommand<bool>;
