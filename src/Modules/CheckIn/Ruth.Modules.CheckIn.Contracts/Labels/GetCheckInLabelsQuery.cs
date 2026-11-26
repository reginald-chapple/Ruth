using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Lists check-in label definitions with optional kind, configuration, and active filters.</summary>
/// <param name="LabelKind">Optional label role filter.</param>
/// <param name="ConfigurationId">Optional check-in configuration filter.</param>
/// <param name="ActiveOnly">Whether to return only active labels.</param>
public sealed record GetCheckInLabelsQuery(
    CheckInLabelKind? LabelKind = null,
    Guid? ConfigurationId = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<CheckInLabelResponse>>;
