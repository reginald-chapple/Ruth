namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Content page published inside a LearningClass.</summary>
/// <param name="Id">Content page identifier.</param>
/// <param name="Title">Page title.</param>
/// <param name="ContentHtml">HTML or safe merge content.</param>
/// <param name="Order">Display order.</param>
/// <param name="StartDateTimeUtc">Optional visibility start timestamp.</param>
public sealed record LearningClassContentPageResponse(
    Guid Id,
    string Title,
    string ContentHtml,
    int Order,
    DateTimeOffset? StartDateTimeUtc
);
