namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Summary of expired SupportRequests archived by cleanup.</summary>
/// <param name="ArchivedCount">Count of expired requests archived.</param>
public sealed record SupportExpirationCleanupResponse(int ArchivedCount);
