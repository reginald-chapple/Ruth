namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Represents a background check request and screening outcome for a person.</summary>
public sealed record BackgroundCheckResponse(
    Guid Id,
    Guid PersonId,
    string ProviderKey,
    string PackageName,
    string? RequestId,
    string? ResponseId,
    Guid? ResponseDocumentBinaryFileId,
    DateTimeOffset RequestedOnUtc,
    DateTimeOffset? RespondedOnUtc,
    BackgroundCheckStatus Status,
    string RawProviderStatus,
    bool? RecordFound,
    string Recommendation,
    DateTimeOffset CreatedOnUtc
);
