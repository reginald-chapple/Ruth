using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Verification;

/// <summary>Query to retrieve the recent profile view audit log for a target person.</summary>
public sealed record GetPersonViewedHistoryQuery(Guid TargetPersonId, int MaxResults = 50)
    : IQuery<IReadOnlyList<PersonViewedResponse>>;
