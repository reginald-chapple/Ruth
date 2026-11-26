using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to post a public comment or internal care note on a SupportRequest.</summary>
/// <param name="SupportRequestId">Target SupportRequest identifier.</param>
/// <param name="AuthorDisplayName">Author display name.</param>
/// <param name="Body">Comment body.</param>
/// <param name="AuthorPersonId">Optional author Person ID.</param>
/// <param name="IsInternalCareNote">Whether visible only to care-team staff.</param>
public sealed record AddSupportRequestCommentCommand(
    Guid SupportRequestId,
    string AuthorDisplayName,
    string Body,
    Guid? AuthorPersonId = null,
    bool IsInternalCareNote = false
) : ICommand<SupportRequestResponse>;
