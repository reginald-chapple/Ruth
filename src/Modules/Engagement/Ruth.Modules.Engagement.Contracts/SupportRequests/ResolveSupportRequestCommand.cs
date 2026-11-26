using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to resolve or record an outcome summary on a SupportRequest.</summary>
/// <param name="SupportRequestId">Target SupportRequest identifier.</param>
/// <param name="OutcomeSummary">Optional resolution or praise summary.</param>
/// <param name="ArchiveImmediately">Whether to archive immediately upon resolution.</param>
public sealed record ResolveSupportRequestCommand(
    Guid SupportRequestId,
    string? OutcomeSummary = null,
    bool ArchiveImmediately = true
) : ICommand<SupportRequestResponse>;
