using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Updates a security role's name, description, or active status.</summary>
public sealed record UpdateSecurityRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    bool IsActive
) : ICommand<SecurityRoleResponse>;
