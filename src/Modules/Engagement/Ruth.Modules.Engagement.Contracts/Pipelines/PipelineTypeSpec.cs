namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Specification for creating or updating an Engagement PipelineType.</summary>
/// <param name="Name">Pipeline display name.</param>
/// <param name="Slug">URL-safe unique pipeline slug.</param>
/// <param name="Stages">Ordered stages in the pipeline.</param>
/// <param name="Description">Optional pipeline description.</param>
/// <param name="EnableFutureFollowUp">Whether requests may be parked until a future follow-up date.</param>
/// <param name="DefaultFollowUpDays">Default follow-up horizon in days.</param>
/// <param name="DaysUntilRequestIdle">Number of days without activity before a request is flagged idle.</param>
/// <param name="RequiresPlacementGroup">Whether a placement group is required before connecting.</param>
/// <param name="IsSequentialStageProgression">Whether stages must advance one step forward at a time.</param>
/// <param name="IsActive">Whether the pipeline is active.</param>
/// <param name="ActivityTypes">Optional pipeline activity types.</param>
/// <param name="Attributes">Optional extensible attribute values.</param>
public sealed record PipelineTypeSpec(
    string Name,
    string Slug,
    IReadOnlyList<PipelineStageSpec> Stages,
    string? Description = null,
    bool EnableFutureFollowUp = true,
    int DefaultFollowUpDays = 7,
    int DaysUntilRequestIdle = 14,
    bool RequiresPlacementGroup = false,
    bool IsSequentialStageProgression = false,
    bool IsActive = true,
    IReadOnlyList<PipelineActivityTypeSpec>? ActivityTypes = null,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
