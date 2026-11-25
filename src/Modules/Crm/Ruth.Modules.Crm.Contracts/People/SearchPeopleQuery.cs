using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Fast pg_trgm, phonetic Double Metaphone, nickname, phone, and search-key person search query.</summary>
public sealed record SearchPeopleQuery(
    string? Query = null,
    PersonRecordStatus? RecordStatus = null,
    PersonConnectionStatus? ConnectionStatus = null,
    Guid? PrimaryBranchId = null,
    bool IncludeDeceased = false,
    bool IncludeBusinesses = false,
    int MaxResults = 25
) : IQuery<IReadOnlyList<PersonSummaryResponse>>;
