namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Threaded comment or internal care note on a SupportRequest.</summary>
/// <param name="Id">Comment identifier.</param>
/// <param name="AuthorPersonId">Optional author Person identifier.</param>
/// <param name="AuthorDisplayName">Display name of comment author.</param>
/// <param name="Body">Comment text.</param>
/// <param name="IsInternalCareNote">Whether restricted to the internal care team.</param>
/// <param name="CreatedAtUtc">Creation UTC timestamp.</param>
public sealed record SupportRequestCommentResponse(
    Guid Id,
    Guid? AuthorPersonId,
    string AuthorDisplayName,
    string Body,
    bool IsInternalCareNote,
    DateTimeOffset CreatedAtUtc
);
