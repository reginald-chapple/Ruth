namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Supported Zebra ZPL II thermal printhead resolutions in dots per inch.</summary>
public enum LabelDpi
{
    /// <summary>203 DPI (8 dots/mm) standard thermal printhead.</summary>
    Dpi203 = 203,

    /// <summary>300 DPI (12 dots/mm) high-resolution thermal printhead.</summary>
    Dpi300 = 300,
}
