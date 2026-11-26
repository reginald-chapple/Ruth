namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Rendered HTML and plain-text output produced by previewing structured email sections.</summary>
/// <param name="RenderedSubject">Evaluated subject line.</param>
/// <param name="RenderedHtml">Full rendered HTML document.</param>
/// <param name="RenderedPlainText">Plain-text fallback representation.</param>
public sealed record EmailComposerPreviewResponse(
    string RenderedSubject,
    string RenderedHtml,
    string RenderedPlainText
);
