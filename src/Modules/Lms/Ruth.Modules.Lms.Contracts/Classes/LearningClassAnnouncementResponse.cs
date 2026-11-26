namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Announcement posted to a LearningClass.</summary>
/// <param name="Id">Announcement identifier.</param>
/// <param name="Title">Title.</param>
/// <param name="Summary">Short summary.</param>
/// <param name="DescriptionHtml">Body HTML.</param>
/// <param name="PublishDateTimeUtc">Publish timestamp in UTC.</param>
public sealed record LearningClassAnnouncementResponse(
    Guid Id,
    string Title,
    string? Summary,
    string DescriptionHtml,
    DateTimeOffset PublishDateTimeUtc
);
