using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Updates an existing check-in badge or receipt label definition.</summary>
/// <param name="LabelId">Target label identifier.</param>
/// <param name="Name">Display name of the label.</param>
/// <param name="Key">Unique slug key.</param>
/// <param name="LabelKind">Functional role of the label.</param>
/// <param name="Layout">Structured visual/ZPL layout schema.</param>
/// <param name="Description">Optional description.</param>
/// <param name="ConfigurationId">Optional scoped check-in configuration identifier.</param>
/// <param name="AreaId">Optional scoped check-in area identifier.</param>
/// <param name="CustomZplOverride">Optional expert raw ZPL II template override.</param>
/// <param name="PrintOrder">Ascending print sequence order.</param>
/// <param name="Copies">Number of copies per trigger (1..5).</param>
/// <param name="IsActive">Whether the label is active.</param>
public sealed record UpdateCheckInLabelCommand(
    Guid LabelId,
    string Name,
    string Key,
    CheckInLabelKind LabelKind,
    CheckInLabelLayoutDto Layout,
    string? Description = null,
    Guid? ConfigurationId = null,
    Guid? AreaId = null,
    string? CustomZplOverride = null,
    int PrintOrder = 0,
    int Copies = 1,
    bool IsActive = true
) : ICommand<CheckInLabelResponse>;
