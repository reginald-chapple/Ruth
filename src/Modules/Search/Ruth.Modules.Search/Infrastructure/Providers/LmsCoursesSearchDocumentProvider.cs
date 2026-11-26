using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class LmsCoursesSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "lms.courses";
    public string ModuleName => "Lms";
    public string EntityTypeKey => "Ruth.Lms.LearningCourse";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetLearningCoursesQuery, IReadOnlyList<LearningCourseResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetLearningCoursesQuery(PublicOnly: false),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(c => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        c.Id,
                        c.PublicName,
                        Subtitle: c.CourseCode,
                        Summary: c.Summary,
                        BodyText: $"{c.Name} {c.PublicName} {c.CourseCode} {c.Summary} {c.Description}",
                        Tags: [c.CourseCode, "Course", "LMS"],
                        UrlPath: "/lms/catalog",
                        IsPublic: c.IsPublic,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
