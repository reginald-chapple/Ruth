namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Represents a configured Engagement PipelineType.</summary>
/// <param name="Id">Pipeline type identifier.</param>
/// <param name="Name">Pipeline display name.</param>
/// <param name="Slug">Unique pipeline slug.</param>
/// <param name="Description">Optional pipeline description.</param>
/// <param name="EnableFutureFollowUp">Whether future follow-ups are enabled.</param>
/// <param name="DefaultFollowUpDays">Default follow-up days.</param>
/// <param name="DaysUntilRequestIdle">Default idle threshold in days.</param>
/// <param name="RequiresPlacementGroup">Whether a placement group is required to connect.</param>
/// <param name="IsSequentialStageProgression">Whether stages must progress sequentially.</param>
/// <param name="IsActive">Whether the pipeline is active.</param>
/// <param name="Stages">Ordered pipeline stages.</param>
/// <param name="ActivityTypes">Configured activity types.</param>
/// <param name="Attributes">Extensible attribute values.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record PipelineTypeResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    bool EnableFutureFollowUp,
    int DefaultFollowUpDays,
    int DaysUntilRequestIdle,
    bool RequiresPlacementGroup,
    bool IsSequentialStageProgression,
    bool IsActive,
    IReadOnlyList<PipelineStageResponse> Stages,
    IReadOnlyList<PipelineActivityTypeResponse> ActivityTypes,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc
);
