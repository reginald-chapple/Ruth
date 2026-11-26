namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Specification for creating or updating a JourneyProgram.</summary>
/// <param name="Name">Journey program name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="Statuses">Available milestone statuses (at least one complete status).</param>
/// <param name="MilestoneTypes">Ordered milestone types in the program.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Core Category ID.</param>
/// <param name="DefaultListView">Default UI view mode.</param>
/// <param name="CompletionMode">Program completion mode.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="Attributes">Optional extensible attributes.</param>
public sealed record JourneyProgramSpec(
    string Name,
    string Slug,
    IReadOnlyList<MilestoneStatusSpec> Statuses,
    IReadOnlyList<MilestoneTypeSpec> MilestoneTypes,
    string? Description = null,
    Guid? CategoryId = null,
    JourneyViewMode DefaultListView = JourneyViewMode.Cards,
    JourneyCompletionMode CompletionMode = JourneyCompletionMode.AllRequiredMilestones,
    bool IsActive = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
