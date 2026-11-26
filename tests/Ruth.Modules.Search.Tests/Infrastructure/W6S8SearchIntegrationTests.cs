using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Infrastructure;
using Ruth.Modules.Search.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.Modules.Search.Tests.Infrastructure;

[Collection(SearchFixtures.Name)]
public sealed class W6S8SearchIntegrationTests(SearchContainersFixture containers)
{
    [Fact]
    public async Task Search_WeightedFullText_FuzzyTrigram_AclEnforcement_CollectionsAndConsumers()
    {
        var conn = await containers.CreateIsolatedDatabaseAsync();
        var publicDocId = Guid.CreateVersion7();
        var restrictedDocId = Guid.CreateVersion7();
        var bodyMatchDocId = Guid.CreateVersion7();
        var customProvider = new FakeSearchDocumentProvider(
            "test.library",
            "Lms",
            "Ruth.Lms.LearningCourse",
            [
                new SearchDocumentUpsertSpec(
                    "test.library",
                    "Lms",
                    "Ruth.Lms.LearningCourse",
                    publicDocId,
                    "Stewardship Foundations Workshop",
                    Subtitle: "STWD-101",
                    Summary: "Practical <b>stewardship</b> guide for community treasurers.",
                    Tags: ["stewardship", "finance"],
                    UrlPath: "/lms/catalog",
                    IsPublic: true
                ),
                new SearchDocumentUpsertSpec(
                    "test.library",
                    "Finance",
                    "Ruth.Finance.FinancialAccount",
                    restrictedDocId,
                    "Stewardship Restricted Endowment Ledger",
                    Subtitle: "BOARD-ONLY",
                    Summary: "Confidential stewardship endowment rules.",
                    UrlPath: "/finance/accounts",
                    IsPublic: false
                ),
                new SearchDocumentUpsertSpec(
                    "test.library",
                    "Cms",
                    "Ruth.Cms.ContentChannelItem",
                    bodyMatchDocId,
                    "Annual Community Report",
                    Summary: "Overview of annual milestones.",
                    BodyText: "Appendix notes mention stewardship principles in passing.",
                    UrlPath: "/cms/content",
                    IsPublic: true
                ),
            ]
        );

        var (provider, auth) = await SearchTestHostFactory.CreateAsync(conn, [customProvider]);
        await using var _ = provider;
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var collection = await commands.SendAsync<
            CreateContentCollectionCommand,
            ContentCollectionResponse
        >(
            new CreateContentCollectionCommand(
                "Member Resource Hub",
                "member-resources",
                [new ContentCollectionSourceSpec("test.library", 100, 1.0)]
            )
        );
        Assert.True(collection.IsSuccess);

        var rebuild1 = await commands.SendAsync<
            RebuildSearchIndexCommand,
            SearchIndexRebuildResponse
        >(new RebuildSearchIndexCommand(collection.Value.Id, "test.library"));
        Assert.True(rebuild1.IsSuccess);
        Assert.Equal(3, rebuild1.Value.DocumentsUpserted);

        // Idempotent SHA-256 second rebuild must skip all 3 unchanged documents
        var rebuild2 = await commands.SendAsync<
            RebuildSearchIndexCommand,
            SearchIndexRebuildResponse
        >(new RebuildSearchIndexCommand(collection.Value.Id, "test.library"));
        Assert.True(rebuild2.IsSuccess);
        Assert.Equal(0, rebuild2.Value.DocumentsUpserted);
        Assert.Equal(3, rebuild2.Value.DocumentsSkippedUnchanged);

        await VerifyRankingAclFuzzyAndSnippetsAsync(
            queries,
            auth,
            publicDocId,
            restrictedDocId,
            bodyMatchDocId
        );
        await VerifyOutboxConsumersAsync(scope.ServiceProvider, queries);
    }

    private static async Task VerifyRankingAclFuzzyAndSnippetsAsync(
        IQueryDispatcher queries,
        StubEntityAuthorizationService auth,
        Guid publicDocId,
        Guid restrictedDocId,
        Guid bodyMatchDocId
    )
    {
        var staffUserId = Guid.CreateVersion7();
        var boardUserId = Guid.CreateVersion7();
        auth.Allow("Ruth.Finance.FinancialAccount", restrictedDocId, boardUserId);

        // Anonymous/public query must exclude restricted doc and rank Weight-A title match above Weight-D body match
        var anonRes = await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
            new ExecuteUniversalSearchQuery(
                "stewardship",
                SecuritySubject.Anonymous,
                CollectionKey: "member-resources",
                PublicOnly: true
            )
        );
        Assert.True(anonRes.IsSuccess);
        Assert.Equal(2, anonRes.Value.TotalAuthorizedHits);
        Assert.Equal(publicDocId, anonRes.Value.Hits[0].EntityId);
        Assert.Equal(bodyMatchDocId, anonRes.Value.Hits[1].EntityId);
        Assert.Contains(
            "<mark>stewardship</mark>",
            anonRes.Value.Hits[0].HighlightedSnippet,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.DoesNotContain(
            "<b>",
            anonRes.Value.Hits[0].HighlightedSnippet,
            StringComparison.OrdinalIgnoreCase
        );

        // Staff without ACL allow does NOT see restricted doc; board member WITH ACL allow DOES see it
        var staffRes = await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
            new ExecuteUniversalSearchQuery(
                "stewardship",
                new SecuritySubject(staffUserId, new HashSet<Guid>()),
                CollectionKey: "member-resources"
            )
        );
        Assert.True(staffRes.IsSuccess);
        Assert.DoesNotContain(staffRes.Value.Hits, h => h.EntityId == restrictedDocId);

        var boardRes = await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
            new ExecuteUniversalSearchQuery(
                "stewardship",
                new SecuritySubject(boardUserId, new HashSet<Guid>()),
                CollectionKey: "member-resources"
            )
        );
        Assert.True(boardRes.IsSuccess);
        Assert.Contains(boardRes.Value.Hits, h => h.EntityId == restrictedDocId);
        Assert.Contains(boardRes.Value.ModuleFacets, static f =>
            string.Equals(f.Value, "Finance", StringComparison.Ordinal)
        );

        // pg_trgm fuzzy typo tolerance ("Stewardshp Foundatons")
        var fuzzyRes = await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
            new ExecuteUniversalSearchQuery(
                "Stewardshp Foundatons",
                SecuritySubject.Anonymous,
                CollectionKey: "member-resources",
                PublicOnly: true
            )
        );
        Assert.True(fuzzyRes.IsSuccess);
        Assert.Contains(fuzzyRes.Value.Hits, h => h.EntityId == publicDocId);
    }

    private static async Task VerifyOutboxConsumersAsync(
        IServiceProvider sp,
        IQueryDispatcher queries
    )
    {
        var db = sp.GetRequiredService<SearchDbContext>();
        var cmsConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<ContentChannelItemPublishedIntegrationEvent>
        >();
        await cmsConsumer.ConsumeAsync(
            new ContentChannelItemPublishedIntegrationEvent(
                DateTimeOffset.UtcNow,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "community-blog",
                "autumn-serve-day",
                "Autumn Serve Day Announcement"
            )
        );

        var lmsConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<LearningCoursePublishedIntegrationEvent>
        >();
        await lmsConsumer.ConsumeAsync(
            new LearningCoursePublishedIntegrationEvent(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "MENT-301",
                "Peer Mentoring Practicum",
                "Hands-on mentoring cohort",
                IsPublic: true
            )
        );

        var search = await queries.QueryAsync<ExecuteUniversalSearchQuery, SearchResultsResponse>(
            new ExecuteUniversalSearchQuery(
                "Practicum",
                SecuritySubject.Anonymous,
                PublicOnly: true
            )
        );
        Assert.True(search.IsSuccess);
        _ = Assert.Single(search.Value.Hits);

        // Verify PersonMerged consumer removes retired person search document
        var retiredPersonId = Guid.CreateVersion7();
        var commands = sp.GetRequiredService<ICommandDispatcher>();
        _ = await commands.SendAsync<UpsertSearchDocumentsCommand, SearchIndexRebuildResponse>(
            new UpsertSearchDocumentsCommand([
                new SearchDocumentUpsertSpec(
                    "crm.people",
                    "Crm",
                    "Ruth.Crm.Person",
                    retiredPersonId,
                    "Duplicate Person Record",
                    IsPublic: false
                ),
            ])
        );
        var mergeConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await mergeConsumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                Guid.CreateVersion7(),
                retiredPersonId,
                [],
                HadMultipleDistinctEmails: false
            )
        );
        Assert.Equal(
            0,
            await db.SearchDocuments.AsNoTracking().CountAsync(d => d.EntityId == retiredPersonId)
        );
    }
}
