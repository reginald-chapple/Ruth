using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to archive SupportRequests past their ExpiresAtUtc timestamp.</summary>
public sealed record ExpireSupportRequestsCommand : ICommand<SupportExpirationCleanupResponse>;
