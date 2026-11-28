using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain.Indexing;
using Ruth.Modules.Search.Domain.Querying;
using Xunit;

namespace Ruth.Modules.Search.Tests.Domain;

public sealed class W8S4SearchSnippetAndUrlSanitizerTests
{
    [Theory]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)><script>evil()</script></svg>")]
    [InlineData("<a href=\"javascript:alert(1)\">click</a>")]
    [InlineData("'\"><iframe srcdoc=\"<script>alert(1)</script>\"></iframe>")]
    public void SanitizeHighlightedSnippet_HtmlEncodesAllOwaspPayloads_AndEmitsOnlyMarkTags(
        string xssVector
    )
    {
        var rawHeadline =
            $"{xssVector} {SearchSnippetSanitizer.StartMarker}scholarship{SearchSnippetSanitizer.StopMarker} grant";
        var sanitized = SearchSnippetSanitizer.SanitizeHighlightedSnippet(rawHeadline);

        Assert.DoesNotContain("<img", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a ", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<mark>scholarship</mark>", sanitized, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizeHighlightedSnippet_NeutralizesUnbalancedOrNestedMarkerSpoofing()
    {
        var unbalanced =
            $"Unclosed {SearchSnippetSanitizer.StartMarker}first {SearchSnippetSanitizer.StartMarker}nested{SearchSnippetSanitizer.StopMarker} orphan {SearchSnippetSanitizer.StopMarker} trailing {SearchSnippetSanitizer.StartMarker}dangling";
        var sanitized = SearchSnippetSanitizer.SanitizeHighlightedSnippet(unbalanced);

        var openCount = CountOccurrences(sanitized, "<mark>");
        var closeCount = CountOccurrences(sanitized, "</mark>");
        Assert.Equal(openCount, closeCount);
        Assert.Equal(1, openCount);
        Assert.DoesNotContain(
            SearchSnippetSanitizer.StartMarker,
            sanitized,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            SearchSnippetSanitizer.StopMarker,
            sanitized,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("javascript:alert(document.cookie)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example/protocol-relative")]
    [InlineData("/\\evil.example/backslash")]
    [InlineData("/courses/aid\r\nInjected: 1")]
    public void SanitizeUrlPath_BlocksDangerousUrls_InQueryAndDocumentDomain(string dangerousUrl)
    {
        Assert.Null(SearchSnippetSanitizer.SanitizeUrlPath(dangerousUrl));

        var docResult = SearchDocument.Create(
            new SearchDocumentUpsertSpec(
                "cms.pages",
                "Cms",
                "Ruth.Cms.Page",
                Guid.CreateVersion7(),
                "Community Assistance",
                UrlPath: dangerousUrl,
                IsPublic: true
            ),
            DateTimeOffset.UtcNow
        );
        Assert.True(docResult.IsSuccess);
        Assert.Null(docResult.Value.UrlPath);
    }

    [Fact]
    public void SanitizeUrlPath_PreservesValidRootRelativeAndHttpsPaths()
    {
        const string validPath = "/programs/food-pantry?branch=north#hours";
        Assert.Equal(validPath, SearchSnippetSanitizer.SanitizeUrlPath(validPath));
        Assert.Equal(
            "https://charity.org/aid",
            SearchSnippetSanitizer.SanitizeUrlPath("https://charity.org/aid")
        );
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += token.Length;
        }

        return count;
    }
}
