namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Gender distribution and nickname expansion insights for a given first name.</summary>
public sealed record MetaNameInsightsResponse(
    string FirstName,
    Gender PredictedGender,
    decimal? MalePercent,
    decimal? FemalePercent,
    IReadOnlyList<string> RelatedNickNames,
    string PrimaryMetaphone,
    string? AlternateMetaphone
);
