using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Crm.Contracts.Events;

/// <summary>
/// Outbox integration event published when a retired person record is merged into a surviving person record,
/// enabling downstream modules (Core, Groups, Finance, CheckIn, etc.) to repoint PersonId references idempotently.
/// </summary>
public sealed record PersonMergedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid SurvivingPersonId,
    Guid RetiredPersonId,
    IReadOnlyList<Guid> ExternalRecordGuids,
    bool HadMultipleDistinctEmails
) : IIntegrationEvent;
