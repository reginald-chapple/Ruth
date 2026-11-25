namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Result of a completed person merge operation.</summary>
public sealed record PersonMergeResponse(
    Guid MergeBatchId,
    Guid SurvivingPersonId,
    Guid RetiredPersonId,
    Guid RedirectId,
    int PhonesTransferred,
    int HouseholdsRepointed,
    int RelationshipsRepointed,
    bool HadMultipleDistinctEmails,
    DateTimeOffset MergedOnUtc
);
