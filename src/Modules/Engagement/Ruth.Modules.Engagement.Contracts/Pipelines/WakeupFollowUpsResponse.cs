namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Summary of scheduled engagement follow-ups woken back into Active state.</summary>
/// <param name="WokenCount">Number of requests transitioned from FutureFollowUp to Active.</param>
public sealed record WakeupFollowUpsResponse(int WokenCount);
