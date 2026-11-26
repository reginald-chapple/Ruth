using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Compiles structured <see cref="EmailSectionDto" /> blocks with merge fields and optional Razor layout into HTML.</summary>
/// <param name="SubjectTemplate">Subject line merge template.</param>
/// <param name="Sections">Ordered structured email sections.</param>
/// <param name="PreheaderText">Optional inbox preheader text.</param>
/// <param name="RazorLayoutKey">Optional compiled Razor layout key.</param>
/// <param name="SampleMergeFields">Optional sample merge values for preview.</param>
/// <param name="IncludeUnsubscribeFooter">Whether to append an unsubscribe and preference link block.</param>
public sealed record PreviewEmailComposerCommand(
    string SubjectTemplate,
    IReadOnlyList<EmailSectionDto> Sections,
    string? PreheaderText = null,
    string? RazorLayoutKey = "Communication.StandardEmail",
    IReadOnlyDictionary<string, string?>? SampleMergeFields = null,
    bool IncludeUnsubscribeFooter = true
) : ICommand<EmailComposerPreviewResponse>;
