using Ruth.Modules.CheckIn.Contracts.Labels;

namespace Ruth.Modules.CheckIn.Domain.Labels;

internal sealed record CheckInLabelSpec(
    string Name,
    string Key,
    CheckInLabelKind LabelKind,
    CheckInLabelLayoutDto Layout,
    string? Description,
    Guid? ConfigurationId,
    Guid? AreaId,
    string? CustomZplOverride,
    int PrintOrder,
    int Copies,
    bool IsActive,
    bool IsSystem = false
);
