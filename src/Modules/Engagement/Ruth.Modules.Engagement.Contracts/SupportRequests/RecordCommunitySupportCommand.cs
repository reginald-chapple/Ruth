using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to increment the CommunitySupportCount on an active SupportRequest.</summary>
/// <param name="SupportRequestId">Target SupportRequest identifier.</param>
/// <param name="IsCareTeamMember">Whether invoked from the internal Care-Team Review Queue.</param>
public sealed record RecordCommunitySupportCommand(
    Guid SupportRequestId,
    bool IsCareTeamMember = false
) : ICommand<SupportRequestResponse>;
