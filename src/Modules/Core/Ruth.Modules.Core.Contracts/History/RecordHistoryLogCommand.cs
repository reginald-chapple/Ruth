using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.History;

/// <summary>Records a structured domain change or audit timeline event.</summary>
public sealed record RecordHistoryLogCommand(
    Guid EntityTypeId,
    Guid EntityId,
    string Verb,
    string ChangeType = "Property",
    string? Caption = null,
    string? ValueName = null,
    string? OldValue = null,
    string? NewValue = null,
    bool IsSensitive = false,
    Guid? CategoryId = null,
    Guid? RelatedEntityTypeId = null,
    Guid? RelatedEntityId = null,
    string? RelatedData = null,
    string? SourceOfChange = null,
    Guid? ChangedByUserId = null
) : ICommand<HistoryLogEntryResponse>;
