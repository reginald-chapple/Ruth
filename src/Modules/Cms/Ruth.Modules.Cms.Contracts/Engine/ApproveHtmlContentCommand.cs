using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to approve or unapprove a specific versioned HTML content revision.</summary>
public sealed record ApproveHtmlContentCommand(
    Guid HtmlContentId,
    bool IsApproved,
    Guid? ApprovedByPersonId = null
) : ICommand<HtmlContentResponse>;
