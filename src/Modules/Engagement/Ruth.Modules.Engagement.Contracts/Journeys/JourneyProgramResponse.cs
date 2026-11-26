namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Represents a configured JourneyProgram and its milestone steps.</summary>
/// <param name="Id">JourneyProgram identifier.</param>
/// <param name="Name">Program name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Category ID.</param>
/// <param name="DefaultListView">Default UI layout.</param>
/// <param name="CompletionMode">Completion rule.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Statuses">Ordered milestone statuses.</param>
/// <param name="MilestoneTypes">Ordered milestone types.</param>
/// <param name="Attributes">Extensible attributes.</param>
/// <param name="CreatedOnUtc">Creation UTC timestamp.</param>
public sealed record JourneyProgramResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? CategoryId,
    JourneyViewMode DefaultListView,
    JourneyCompletionMode CompletionMode,
    bool IsActive,
    IReadOnlyList<MilestoneStatusResponse> Statuses,
    IReadOnlyList<MilestoneTypeResponse> MilestoneTypes,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc
);
