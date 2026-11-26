namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Check-in badge or receipt label definition response.</summary>
/// <param name="Id">Label identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="Description">Optional description.</param>
/// <param name="LabelKind">Functional label category.</param>
/// <param name="ConfigurationId">Optional scoped check-in configuration identifier.</param>
/// <param name="AreaId">Optional scoped check-in area identifier.</param>
/// <param name="Layout">Structured visual/ZPL layout schema.</param>
/// <param name="CustomZplOverride">Optional raw ZPL II template override.</param>
/// <param name="PrintOrder">Ascending print sequence order.</param>
/// <param name="Copies">Number of copies to print per trigger (1..5).</param>
/// <param name="IsActive">Whether the label is active.</param>
/// <param name="IsSystem">Whether the label is a protected system default.</param>
/// <param name="CreatedOnUtc">UTC creation timestamp.</param>
/// <param name="ModifiedOnUtc">Optional UTC modification timestamp.</param>
public sealed record CheckInLabelResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    CheckInLabelKind LabelKind,
    Guid? ConfigurationId,
    Guid? AreaId,
    CheckInLabelLayoutDto Layout,
    string? CustomZplOverride,
    int PrintOrder,
    int Copies,
    bool IsActive,
    bool IsSystem,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
