using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Collections;
using Ruth.Modules.Search.Domain.Indexing;
using Ruth.Modules.Search.Domain.Querying;
using Xunit;

namespace Ruth.Modules.Search.Tests.Domain;

public sealed class W6S8SearchDomainTests
{
    [Fact]
    public void ContentCollectionDefinition_NormalizesSlugAndPreventsDuplicateSources()
    {
        var colRes = ContentCollectionDefinition.Create(
            "Community Library",
            "Community Library 2026!",
            "Public search catalog"
        );
        Assert.True(colRes.IsSuccess);
        Assert.Equal("community-library-2026", colRes.Value.CollectionKey);

        var s1 = colRes.Value.AddSource("lms.courses", "Ruth.Lms.LearningCourse", 50, 1.5);
        Assert.True(s1.IsSuccess);

        var dup = colRes.Value.AddSource("LMS.COURSES", "Ruth.Lms.LearningCourse");
        Assert.True(dup.IsFailure);
    }

    [Fact]
    public void SearchDocument_ApplyUpdate_IsIdempotentOnUnchangedContentHash()
    {
        var entityId = Guid.CreateVersion7();
        var spec = new SearchDocumentUpsertSpec(
            "lms.courses",
            "Lms",
            "Ruth.Lms.LearningCourse",
            entityId,
            "Community First Aid",
            Subtitle: "AID-101",
            Summary: "Learn emergency response basics.",
            Tags: ["safety", "Safety", "training"],
            IsPublic: true
        );
        var now = DateTimeOffset.UtcNow;

        var docRes = SearchDocument.Create(spec, now);
        Assert.True(docRes.IsSuccess);
        var doc = docRes.Value;
        Assert.Equal("safety training", doc.TagsText);
        Assert.Equal(64, doc.ContentHash.Length);

        // Identical payload must not mark updated
        Assert.False(doc.ApplyUpdate(spec, now.AddMinutes(5)));

        // Changed summary must recompute hash and return true
        var updatedSpec = spec with { Summary = "Updated CPR and emergency response basics." };
        Assert.True(doc.ApplyUpdate(updatedSpec, now.AddMinutes(10)));
        Assert.Equal("Updated CPR and emergency response basics.", doc.Summary);
    }

    [Fact]
    public void SearchSnippetSanitizer_EscapesHtmlWhilePreservingMarkHighlights()
    {
        var raw =
            $"<script>alert(1)</script> Learn {SearchSnippetSanitizer.StartMarker}leadership{SearchSnippetSanitizer.StopMarker} & mentoring";
        var safe = SearchSnippetSanitizer.SanitizeHighlightedSnippet(raw);

        Assert.DoesNotContain("<script>", safe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", safe, StringComparison.Ordinal);
        Assert.Contains("<mark>leadership</mark>", safe, StringComparison.Ordinal);
        Assert.Contains("&amp; mentoring", safe, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchTextNormalizer_NormalizesSlugsAndRejectsInvalidEmptyDocumentSpecs()
    {
        Assert.Equal("staff-spotlight", SearchTextNormalizer.NormalizeSlug("  Staff_Spotlight!! "));
        var invalid = SearchDocument.Create(
            new SearchDocumentUpsertSpec(
                "",
                "Crm",
                "Ruth.Crm.Person",
                Guid.CreateVersion7(),
                "Ada"
            ),
            DateTimeOffset.UtcNow
        );
        Assert.True(invalid.IsFailure);
    }
}
