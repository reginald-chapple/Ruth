namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Input specification for an activity type logged against pipeline requests.</summary>
/// <param name="Name">Activity type name (e.g., Phone Call, Intro Coffee, Orientation).</param>
/// <param name="Order">Display order.</param>
/// <param name="Id">Optional existing activity type ID when updating.</param>
/// <param name="IsActive">Whether the activity type is active.</param>
public sealed record PipelineActivityTypeSpec(
    string Name,
    int Order,
    Guid? Id = null,
    bool IsActive = true
);
