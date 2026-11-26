namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>JSON-serializable label layout schema compiled into Zebra ZPL II commands.</summary>
/// <param name="WidthInches">Label width in inches (1.0 .. 6.0).</param>
/// <param name="HeightInches">Label height in inches (0.5 .. 8.0).</param>
/// <param name="Dpi">Target thermal printhead DPI.</param>
/// <param name="Elements">Ordered positioned elements on the label.</param>
public sealed record CheckInLabelLayoutDto(
    decimal WidthInches,
    decimal HeightInches,
    LabelDpi Dpi,
    IReadOnlyList<CheckInLabelElementDto> Elements
);
