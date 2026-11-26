using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Infrastructure.Indexing;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Search.Slices.Events;

internal sealed class SearchLearningCoursePublishedConsumer(
    SearchDocumentUpsertService upsertService
) : IIntegrationEventConsumer<LearningCoursePublishedIntegrationEvent>
{
    public async Task ConsumeAsync(
        LearningCoursePublishedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new SearchDocumentUpsertSpec(
            "lms.courses",
            "Lms",
            "Ruth.Lms.LearningCourse",
            integrationEvent.LearningCourseId,
            integrationEvent.Name,
            Subtitle: integrationEvent.CourseCode,
            Summary: integrationEvent.Summary,
            Tags: [integrationEvent.CourseCode, "LMS", "Course"],
            UrlPath: "/lms/catalog",
            IsPublic: integrationEvent.IsPublic
        );
        _ = await upsertService.UpsertBatchAsync([spec], cancellationToken);
    }
}
