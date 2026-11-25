namespace Ruth.SharedKernel.Security;

/// <summary>Represents a securable entity participating in hierarchical Allow/Deny ACL evaluation.</summary>
public interface ISecured
{
    /// <summary>Gets the canonical entity type key (for example, Ruth.Core.DefinedType).</summary>
    string EntityTypeKey { get; }

    /// <summary>Gets the entity instance identifier, or <see cref="Guid.Empty" /> for type/global defaults.</summary>
    Guid EntityId { get; }

    /// <summary>Gets the primary parent authority consulted when no local rule matches.</summary>
    ISecured? ParentAuthority { get; }

    /// <summary>
    /// Gets an optional pre-parent authority evaluated locally (without further recursion)
    /// only for the root entity prior to <see cref="ParentAuthority" />.
    /// </summary>
    ISecured? ParentAuthorityPre => null;

    /// <summary>Gets the supported actions and their human-readable descriptions.</summary>
    IReadOnlyDictionary<string, string> SupportedActions => AuthorizationActions.StandardActions;

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="action" /> is allowed when no explicit rule
    /// matches anywhere in the authorization hierarchy.
    /// </summary>
    bool IsAllowedByDefault(string action);
}
