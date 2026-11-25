using System.ComponentModel.DataAnnotations;
using Ruth.Modules.Identity.Contracts.Security;

namespace Ruth.Modules.Identity.Slices.Security;

/// <summary>Request payload for replacing ordered ACL rules for a secured entity action.</summary>
public sealed record SaveEntityAclRulesRequest
{
    /// <summary>Gets the canonical entity type key.</summary>
    [Required]
    public required string EntityTypeKey { get; init; }

    /// <summary>Gets the entity instance identifier.</summary>
    public Guid EntityId { get; init; }

    /// <summary>Gets the authorization action name.</summary>
    [Required]
    public required string Action { get; init; }

    /// <summary>Gets the ordered ACL rules.</summary>
    [Required]
    public required IReadOnlyList<AclRuleInput> Rules { get; init; }
}
