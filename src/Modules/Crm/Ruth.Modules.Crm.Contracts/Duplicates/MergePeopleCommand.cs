using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>
/// Command to merge a retired person record into a surviving person record, write a PersonRedirect entry,
/// and publish a transactional Outbox PersonMergedIntegrationEvent.
/// </summary>
public sealed record MergePeopleCommand(
    Guid SurvivingPersonId,
    Guid RetiredPersonId,
    PersonMergeFieldSelections? FieldSelections = null,
    bool HasElevatedMergeAuthorization = false,
    Guid? MergedByUserId = null,
    Guid? ExternalRecordGuid = null
) : ICommand<PersonMergeResponse>;
