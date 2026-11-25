namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Controls how interpolated merge token values are encoded in rendered output.</summary>
public enum TemplateOutputMode
{
    /// <summary>HTML-encodes all interpolated context values while preserving authored template markup.</summary>
    Html = 0,

    /// <summary>Emits plain text without HTML encoding (suitable for SMS, ZPL labels, and plain-text emails).</summary>
    PlainText = 1,
}
