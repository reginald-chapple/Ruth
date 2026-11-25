using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Query to list detected potential duplicate person pairs filtered by confidence threshold and status.</summary>
public sealed record GetPersonDuplicatesQuery(
    decimal MinConfidenceScore = 40m,
    bool IncludeIgnored = false,
    bool IncludeConfirmedNotDuplicate = false,
    int MaxResults = 100
) : IQuery<IReadOnlyList<PersonDuplicateResponse>>;
