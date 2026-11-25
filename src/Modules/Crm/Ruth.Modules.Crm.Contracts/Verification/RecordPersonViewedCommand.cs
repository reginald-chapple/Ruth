using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Command to record a PersonViewed audit entry and increment Person.ViewedCount.</summary>
public sealed record RecordPersonViewedCommand(
    Guid TargetPersonId,
    Guid? ViewerPersonId = null,
    Guid? ViewerUserId = null,
    string? IpAddress = null,
    string? Source = null
) : ICommand<PersonViewedResponse>;
