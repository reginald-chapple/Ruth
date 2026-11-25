using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Command to mark a duplicate pair as confirmed not-duplicate or ignored until score changes.</summary>
public sealed record MarkDuplicateStatusCommand(
    Guid DuplicateId,
    bool IsConfirmedAsNotDuplicate,
    bool IgnoreUntilScoreChanges
) : ICommand<PersonDuplicateResponse>;
