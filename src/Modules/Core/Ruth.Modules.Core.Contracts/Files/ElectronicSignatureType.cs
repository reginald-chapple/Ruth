namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Electronic signature input method supported by a template.</summary>
public enum ElectronicSignatureType
{
    /// <summary>Signer types their legal name.</summary>
    Typed = 0,

    /// <summary>Signer draws a PNG signature on a canvas.</summary>
    Drawn = 1,
}
