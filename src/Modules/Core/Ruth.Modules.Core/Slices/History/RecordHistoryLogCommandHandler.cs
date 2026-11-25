using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.History;

internal sealed class RecordHistoryLogCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<RecordHistoryLogCommand, HistoryLogEntryResponse>
{
    public async Task<Result<HistoryLogEntryResponse>> HandleAsync(
        RecordHistoryLogCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = HistoryLogEntry.Create(
            EntityTypeDescriptorId.From(command.EntityTypeId),
            command.EntityId,
            command.Verb,
            command.ChangeType,
            command.Caption,
            command.ValueName,
            command.OldValue,
            command.NewValue,
            command.IsSensitive,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.RelatedEntityTypeId.HasValue
                ? EntityTypeDescriptorId.From(command.RelatedEntityTypeId.Value)
                : null,
            command.RelatedEntityId,
            command.RelatedData,
            command.SourceOfChange,
            command.ChangedByUserId,
            clock.UtcNow
        );
        if (created.IsFailure)
        {
            return Result.Failure<HistoryLogEntryResponse>(created.Errors);
        }

        _ = dbContext.HistoryLogEntries.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(HistoryMapper.ToResponse(created.Value));
    }
}
