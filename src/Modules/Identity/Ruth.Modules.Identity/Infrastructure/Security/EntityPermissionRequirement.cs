using Microsoft.AspNetCore.Authorization;

namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>ASP.NET Core authorization requirement verifying hierarchical ACL access for an action on an ISecured resource.</summary>
public sealed class EntityPermissionRequirement(string action) : IAuthorizationRequirement
{
    /// <summary>Gets the required authorization action name.</summary>
    public string Action { get; } =
        string.IsNullOrWhiteSpace(action)
            ? throw new ArgumentException("Action is required.", nameof(action))
            : action.Trim();
}
