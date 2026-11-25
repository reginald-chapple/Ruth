namespace Ruth.Modules.Core.Contracts.History;

/// <summary>Structured domain history/audit timeline entry.</summary>
public sealed record HistoryLogEntryResponse(
    Guid Id,
    Guid EntityTypeId,
    Guid EntityId,
    Guid? CategoryId,
    string Verb,
    string ChangeType,
    string? Caption,
    string? ValueName,
    string? OldValue,
    string? NewValue,
    bool IsSensitive,
    string Summary,
    Guid? RelatedEntityTypeId,
    Guid? RelatedEntityId,
    string? RelatedData,
    string? SourceOfChange,
    Guid? ChangedByUserId,
    DateTimeOffset OccurredOnUtc
);
