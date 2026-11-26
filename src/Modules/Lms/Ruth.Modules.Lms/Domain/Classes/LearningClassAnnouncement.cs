using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>Announcement posted to learners in a LearningClass.</summary>
public sealed class LearningClassAnnouncement : Entity<LearningClassAnnouncementId>
{
    private LearningClassAnnouncement() { }

    private LearningClassAnnouncement(LearningClassAnnouncementId id)
        : base(id) { }

    public LearningClassId LearningClassId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string DescriptionHtml { get; private set; } = string.Empty;
    public DateTimeOffset PublishDateTimeUtc { get; private set; }

    public static Result<LearningClassAnnouncement> Create(
        LearningClassId classId,
        string title,
        string descriptionHtml,
        DateTimeOffset publishDateTimeUtc,
        string? summary = null
    )
    {
        return string.IsNullOrWhiteSpace(title)
            ? Result.Failure<LearningClassAnnouncement>(
                LmsErrors.Validation(
                    "Announcement.TitleRequired",
                    "Announcement title is required."
                )
            )
            : Result.Success(
                new LearningClassAnnouncement(LearningClassAnnouncementId.Create())
                {
                    LearningClassId = classId,
                    Title = title.Trim(),
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                    DescriptionHtml = descriptionHtml ?? string.Empty,
                    PublishDateTimeUtc = publishDateTimeUtc.ToUniversalTime(),
                }
            );
    }
}
