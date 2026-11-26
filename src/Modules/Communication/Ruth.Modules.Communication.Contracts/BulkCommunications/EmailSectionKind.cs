namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Structured section block types supported by the visual email composer.</summary>
public enum EmailSectionKind
{
    /// <summary>Full-width hero banner with heading and lead copy.</summary>
    HeroBanner = 0,

    /// <summary>Merge-enabled rich text or HTML paragraph block.</summary>
    RichText = 1,

    /// <summary>Primary call-to-action button block.</summary>
    CallToActionButton = 2,

    /// <summary>Responsive image block with mandatory alt text.</summary>
    ImageBlock = 3,

    /// <summary>Side-by-side two-column content section.</summary>
    TwoColumnText = 4,

    /// <summary>Horizontal visual divider rule.</summary>
    Divider = 5,

    /// <summary>Organization footer with optional preference/unsubscribe links.</summary>
    Footer = 6,
}
