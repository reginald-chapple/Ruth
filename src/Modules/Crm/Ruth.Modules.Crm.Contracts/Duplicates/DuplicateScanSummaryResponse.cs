namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Summary metrics produced by a duplicate detection scan.</summary>
public sealed record DuplicateScanSummaryResponse(
    int PeopleScanned,
    int PairsCreated,
    int PairsUpdated,
    int StalePairsRemoved,
    DateTimeOffset CompletedOnUtc
);
