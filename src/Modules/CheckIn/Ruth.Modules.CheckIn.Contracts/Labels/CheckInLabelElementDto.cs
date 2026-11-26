namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Positioned element inside a structured check-in label layout schema.</summary>
/// <param name="ElementKey">Unique key within the label layout.</param>
/// <param name="Kind">Element primitive kind.</param>
/// <param name="X">Horizontal origin in dots from top-left.</param>
/// <param name="Y">Vertical origin in dots from top-left.</param>
/// <param name="Width">Bounding width in dots.</param>
/// <param name="Height">Bounding height in dots.</param>
/// <param name="FontHeight">Scalable font height in dots.</param>
/// <param name="FontWidth">Scalable font width in dots (0 = proportional).</param>
/// <param name="MaxLines">Maximum wrapped lines inside the field block.</param>
/// <param name="Alignment">Horizontal text alignment.</param>
/// <param name="InvertPrint">Whether to print inverted white-on-black (^FR over ^GB).</param>
/// <param name="ContentTemplate">Text or merge-field template (e.g. "{{ Participant.NickName }}").</param>
/// <param name="ShowOnlyWhenFieldPresent">Optional context field key required to be non-empty before rendering.</param>
public sealed record CheckInLabelElementDto(
    string ElementKey,
    LabelElementKind Kind,
    int X,
    int Y,
    int Width,
    int Height,
    int FontHeight = 32,
    int FontWidth = 0,
    int MaxLines = 1,
    LabelTextAlignment Alignment = LabelTextAlignment.Left,
    bool InvertPrint = false,
    string ContentTemplate = "",
    string? ShowOnlyWhenFieldPresent = null
);
