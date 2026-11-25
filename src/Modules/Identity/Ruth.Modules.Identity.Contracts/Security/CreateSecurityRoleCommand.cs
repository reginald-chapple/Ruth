using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Creates a new custom security role.</summary>
public sealed record CreateSecurityRoleCommand(string Name, string? Description = null)
    : ICommand<SecurityRoleResponse>;
