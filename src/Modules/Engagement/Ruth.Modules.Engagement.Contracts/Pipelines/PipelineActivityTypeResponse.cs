namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Represents an activity type configured on a PipelineType.</summary>
/// <param name="Id">Activity type identifier.</param>
/// <param name="Name">Activity type display name.</param>
/// <param name="Order">Display order.</param>
/// <param name="IsActive">Whether the activity type is active.</param>
public sealed record PipelineActivityTypeResponse(Guid Id, string Name, int Order, bool IsActive);
