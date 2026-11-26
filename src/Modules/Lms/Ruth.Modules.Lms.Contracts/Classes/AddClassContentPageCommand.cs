using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Adds a content page to a LearningClass.</summary>
/// <param name="LearningClassId">Class identifier.</param>
/// <param name="Title">Page title.</param>
/// <param name="ContentHtml">HTML content body.</param>
/// <param name="Order">Sort order.</param>
/// <param name="StartDateTimeUtc">Optional visibility start timestamp.</param>
public sealed record AddClassContentPageCommand(
    Guid LearningClassId,
    string Title,
    string ContentHtml,
    int Order = 0,
    DateTimeOffset? StartDateTimeUtc = null
) : ICommand<LearningClassContentPageResponse>;
