using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Lists all security roles.</summary>
public sealed record GetSecurityRolesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<SecurityRoleResponse>>;
