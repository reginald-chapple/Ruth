using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to flag a public SupportRequest for care-team review.</summary>
/// <param name="SupportRequestId">Target SupportRequest identifier.</param>
public sealed record FlagSupportRequestCommand(Guid SupportRequestId)
    : ICommand<SupportRequestResponse>;
