namespace Ruth.Modules.Identity.Domain;

/// <summary>Diagnostic explanation describing how an ACL decision was reached.</summary>
public sealed record AclEvaluationTrace(
    bool IsAllowed,
    string Source,
    string? MatchedEntityTypeKey,
    Guid? MatchedEntityId,
    Guid? MatchedRuleId,
    int? MatchedOrder,
    string Explanation
);
