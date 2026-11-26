using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Publishes an announcement to a LearningClass.</summary>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="Title">Announcement title.</param>
/// <param name="DescriptionHtml">Body HTML.</param>
/// <param name="Summary">Optional summary.</param>
/// <param name="PublishDateTimeUtc">Optional publish timestamp (defaults to now).</param>
public sealed record AddClassAnnouncementCommand(
    Guid LearningClassId,
    string Title,
    string DescriptionHtml,
    string? Summary = null,
    DateTimeOffset? PublishDateTimeUtc = null
) : ICommand<LearningClassAnnouncementResponse>;
