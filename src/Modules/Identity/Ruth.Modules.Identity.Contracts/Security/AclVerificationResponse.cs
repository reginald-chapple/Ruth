namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Explains the outcome and matching rule/inheritance source of an ACL evaluation.</summary>
public sealed record AclVerificationResponse(
    bool IsAllowed,
    string Source,
    string? MatchedEntityTypeKey,
    Guid? MatchedEntityId,
    Guid? MatchedRuleId,
    int? MatchedOrder,
    string Explanation
);
