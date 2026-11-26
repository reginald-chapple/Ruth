using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Compiles either a persisted label or an inline draft layout into Zebra ZPL II and visual preview elements.</summary>
/// <param name="LabelId">Optional persisted label identifier.</param>
/// <param name="DraftLayout">Optional inline draft layout (used when <paramref name="LabelId" /> is null).</param>
/// <param name="CustomZplOverride">Optional raw ZPL override.</param>
/// <param name="Copies">Number of copies (1..5).</param>
/// <param name="Context">Sample or live participant merge-field context.</param>
public sealed record PreviewCheckInLabelCommand(
    Guid? LabelId = null,
    CheckInLabelLayoutDto? DraftLayout = null,
    string? CustomZplOverride = null,
    int Copies = 1,
    CheckInLabelMergeContextDto? Context = null
) : ICommand<CheckInLabelPreviewResponse>;
