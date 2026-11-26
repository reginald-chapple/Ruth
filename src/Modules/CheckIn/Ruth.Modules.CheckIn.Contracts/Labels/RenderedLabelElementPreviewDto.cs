namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Visual WYSIWYG preview element positioned by percentage and dot coordinates.</summary>
/// <param name="ElementKey">Element key.</param>
/// <param name="Kind">Element primitive kind.</param>
/// <param name="X">X origin in dots.</param>
/// <param name="Y">Y origin in dots.</param>
/// <param name="Width">Width in dots.</param>
/// <param name="Height">Height in dots.</param>
/// <param name="LeftPercent">CSS left percentage (0..100).</param>
/// <param name="TopPercent">CSS top percentage (0..100).</param>
/// <param name="WidthPercent">CSS width percentage (0..100).</param>
/// <param name="HeightPercent">CSS height percentage (0..100).</param>
/// <param name="Alignment">Text alignment.</param>
/// <param name="InvertPrint">Whether rendered inverted white-on-black.</param>
/// <param name="ResolvedText">Evaluated display text after merge-field substitution.</param>
public sealed record RenderedLabelElementPreviewDto(
    string ElementKey,
    LabelElementKind Kind,
    int X,
    int Y,
    int Width,
    int Height,
    decimal LeftPercent,
    decimal TopPercent,
    decimal WidthPercent,
    decimal HeightPercent,
    LabelTextAlignment Alignment,
    bool InvertPrint,
    string ResolvedText
);
