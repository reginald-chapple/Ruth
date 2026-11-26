using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Domain.Classes;

/// <summary>Structured content page published inside a LearningClass.</summary>
public sealed class LearningClassContentPage : Entity<LearningClassContentPageId>
{
    private LearningClassContentPage() { }

    private LearningClassContentPage(LearningClassContentPageId id)
        : base(id) { }

    public LearningClassId LearningClassId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ContentHtml { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public DateTimeOffset? StartDateTimeUtc { get; private set; }

    public static Result<LearningClassContentPage> Create(
        LearningClassId classId,
        string title,
        string contentHtml,
        int order = 0,
        DateTimeOffset? startDateTimeUtc = null
    )
    {
        return string.IsNullOrWhiteSpace(title)
            ? Result.Failure<LearningClassContentPage>(
                LmsErrors.Validation("ContentPage.TitleRequired", "Content page title is required.")
            )
            : Result.Success(
                new LearningClassContentPage(LearningClassContentPageId.Create())
                {
                    LearningClassId = classId,
                    Title = title.Trim(),
                    ContentHtml = contentHtml ?? string.Empty,
                    Order = order,
                    StartDateTimeUtc = startDateTimeUtc?.ToUniversalTime(),
                }
            );
    }
}
