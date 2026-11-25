using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Slices.History;

internal static class HistoryMapper
{
    public static HistoryLogEntryResponse ToResponse(HistoryLogEntry e)
    {
        return new HistoryLogEntryResponse(
            e.Id.Value,
            e.EntityTypeId.Value,
            e.EntityId,
            e.CategoryId?.Value,
            e.Verb,
            e.ChangeType,
            e.Caption,
            e.ValueName,
            e.OldValue,
            e.NewValue,
            e.IsSensitive,
            e.FormatSummary(),
            e.RelatedEntityTypeId?.Value,
            e.RelatedEntityId,
            e.RelatedData,
            e.SourceOfChange,
            e.ChangedByUserId,
            e.OccurredOnUtc
        );
    }
}
