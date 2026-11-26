namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Structured section definition within an email template or bulk communication.</summary>
/// <param name="Order">Zero-based display order of the section.</param>
/// <param name="Kind">Visual section block type.</param>
/// <param name="Heading">Optional section heading text (supports safe merge tokens).</param>
/// <param name="BodyMarkup">Optional primary body markup (supports safe merge tokens).</param>
/// <param name="ButtonLabel">Optional call-to-action button caption.</param>
/// <param name="ButtonUrl">Optional safe target URL or merge token for the CTA button.</param>
/// <param name="SecondaryColumnMarkup">Optional right-column markup for two-column sections.</param>
/// <param name="ImageUrl">Optional safe image source URL for image sections.</param>
/// <param name="ImageAltText">Accessible alternative text for image sections.</param>
/// <param name="AccentColorHex">Optional 6-digit hex accent color (e.g., #0f6cbd).</param>
public sealed record EmailSectionDto(
    int Order,
    EmailSectionKind Kind,
    string? Heading = null,
    string? BodyMarkup = null,
    string? ButtonLabel = null,
    string? ButtonUrl = null,
    string? SecondaryColumnMarkup = null,
    string? ImageUrl = null,
    string? ImageAltText = null,
    string? AccentColorHex = null
);
