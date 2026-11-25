namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Allow or Deny outcome of an entity ACL rule.</summary>
public enum AclRuleDecision
{
    /// <summary>Explicitly grants permission for the action.</summary>
    Allow = 1,

    /// <summary>Explicitly denies permission for the action.</summary>
    Deny = 2,
}
