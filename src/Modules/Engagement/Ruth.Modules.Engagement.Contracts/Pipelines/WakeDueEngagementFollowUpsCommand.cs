using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to wake FutureFollowUp EngagementRequests whose follow-up date is on or before today.</summary>
public sealed record WakeDueEngagementFollowUpsCommand : ICommand<WakeupFollowUpsResponse>;
