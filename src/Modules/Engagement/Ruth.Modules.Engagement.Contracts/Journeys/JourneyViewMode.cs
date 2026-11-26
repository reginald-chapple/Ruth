namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Preferred UI display layout for a JourneyProgram.</summary>
public enum JourneyViewMode
{
    /// <summary>Card-based step pathway.</summary>
    Cards = 0,

    /// <summary>Chronological milestone timeline.</summary>
    Timeline = 1,

    /// <summary>Tabular grid view.</summary>
    Grid = 2,
}
