namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Summary returned after executing a GroupSync reconciliation pass.</summary>
public sealed record GroupSyncExecutionResponse(
    Guid RuleId,
    Guid GroupId,
    int SourcePersonCount,
    int MembersAdded,
    int MembersRemoved,
    int MembersSkipped,
    DateTimeOffset ExecutedOnUtc
);
