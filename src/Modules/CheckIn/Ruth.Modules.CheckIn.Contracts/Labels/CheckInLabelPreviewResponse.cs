namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Compiled Zebra ZPL II payload and visual WYSIWYG preview elements for a check-in label.</summary>
/// <param name="LabelName">Label display name.</param>
/// <param name="WidthDots">Total label width in dots.</param>
/// <param name="HeightDots">Total label height in dots.</param>
/// <param name="Dpi">Printhead DPI.</param>
/// <param name="CompiledZpl">Compiled Zebra ZPL II command stream.</param>
/// <param name="VisualElements">Visible positioned elements for WYSIWYG canvas rendering.</param>
public sealed record CheckInLabelPreviewResponse(
    string LabelName,
    int WidthDots,
    int HeightDots,
    LabelDpi Dpi,
    string CompiledZpl,
    IReadOnlyList<RenderedLabelElementPreviewDto> VisualElements
);
