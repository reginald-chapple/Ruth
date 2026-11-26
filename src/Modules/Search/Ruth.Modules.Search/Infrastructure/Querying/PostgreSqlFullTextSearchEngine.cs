using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Querying;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Infrastructure.Querying;

/// <summary>
/// Deterministic PostgreSQL full-text (weighted tsvector + websearch_to_tsquery + ts_rank_cd + ts_headline)
/// and pg_trgm trigram fuzzy search engine with query-time ISecured ACL and facet filtering.
/// </summary>
internal sealed class PostgreSqlFullTextSearchEngine(
    SearchDbContext db,
    IEntityAuthorizationService authorizationService
)
{
    private sealed record CandidateRow(
        Guid DocumentId,
        string ModuleName,
        string EntityTypeKey,
        Guid EntityId,
        string Title,
        string? Subtitle,
        string RawHeadline,
        string? UrlPath,
        double Score,
        bool IsPublic,
        string TagsText,
        string FacetsJson,
        string SecurityEntityTypeKey,
        Guid SecurityEntityId
    );

    public async Task<SearchResultsResponse> SearchAsync(
        ExecuteUniversalSearchQuery query,
        CancellationToken ct
    )
    {
        var rawText = (query.QueryText ?? string.Empty).Trim();
        var limit = Math.Clamp(query.Limit, 1, 100);
        var sw = Stopwatch.StartNew();

        var collectionId = await ResolveCollectionIdAsync(query.CollectionKey, ct);
        if (!string.IsNullOrWhiteSpace(query.CollectionKey) && collectionId is null)
        {
            sw.Stop();
            return new SearchResultsResponse(rawText, 0, (int)sw.ElapsedMilliseconds, [], [], []);
        }

        var candidates = await FetchCandidatesAsync(
            rawText,
            collectionId,
            query.ModuleNames,
            query.EntityTypeKeys,
            query.PublicOnly,
            overReadLimit: Math.Min(300, limit * 3),
            ct
        );

        var subject = query.Subject ?? SecuritySubject.Anonymous;
        var authorized = await FilterByAclAsync(candidates, subject, limit, ct);
        sw.Stop();

        return PostgreSqlFullTextSqlBuilder.BuildResponse(
            rawText,
            authorized,
            (int)sw.ElapsedMilliseconds
        );
    }

    private async Task<ContentCollectionId?> ResolveCollectionIdAsync(
        string? collectionKey,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(collectionKey))
        {
            return null;
        }

        var slug = SearchTextNormalizer.NormalizeSlug(collectionKey);
        return await db
            .ContentCollections.AsNoTracking()
            .Where(c => c.CollectionKey == slug && c.IsActive)
            .Select(static c => (ContentCollectionId?)c.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<List<CandidateRow>> FetchCandidatesAsync(
        string rawText,
        ContentCollectionId? collectionId,
        IReadOnlyList<string>? moduleNames,
        IReadOnlyList<string>? entityTypeKeys,
        bool publicOnly,
        int overReadLimit,
        CancellationToken ct
    )
    {
        var hasQuery = !string.IsNullOrWhiteSpace(rawText);
        var sql = hasQuery
            ? PostgreSqlFullTextSqlBuilder.BuildFullTextSql()
            : PostgreSqlFullTextSqlBuilder.BuildBrowseSql();
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
                conn,
                db.Database.CurrentTransaction?.GetDbTransaction(),
                ct
            );
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            PostgreSqlFullTextSqlBuilder.AddFilterParameters(
                cmd,
                rawText,
                collectionId,
                moduleNames,
                entityTypeKeys,
                publicOnly,
                overReadLimit
            );
            return await ReadCandidatesAsync(cmd, ct);
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<List<CandidateRow>> ReadCandidatesAsync(
        DbCommand cmd,
        CancellationToken ct
    )
    {
        List<CandidateRow> rows = [];
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(
                new CandidateRow(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetGuid(3),
                    reader.GetString(4),
                    await reader.IsDBNullAsync(5, ct) ? null : reader.GetString(5),
                    await reader.IsDBNullAsync(6, ct) ? string.Empty : reader.GetString(6),
                    await reader.IsDBNullAsync(7, ct) ? null : reader.GetString(7),
                    await reader.IsDBNullAsync(8, ct) ? 0.0 : reader.GetDouble(8),
                    !await reader.IsDBNullAsync(9, ct) && reader.GetBoolean(9),
                    await reader.IsDBNullAsync(10, ct) ? string.Empty : reader.GetString(10),
                    await reader.IsDBNullAsync(11, ct) ? "{}" : reader.GetString(11),
                    reader.GetString(12),
                    reader.GetGuid(13)
                )
            );
        }

        return rows;
    }

    private async Task<List<SearchHitDto>> FilterByAclAsync(
        List<CandidateRow> candidates,
        SecuritySubject subject,
        int limit,
        CancellationToken ct
    )
    {
        List<SearchHitDto> authorized = [];
        foreach (var row in candidates)
        {
            var securedRef = new SecuredEntityRef(
                row.SecurityEntityTypeKey,
                row.SecurityEntityId,
                row.IsPublic
                    ? GlobalDefaultSecured.Instance
                    : GlobalRestrictedDefaultSecured.Instance
            );
            var allowed = await authorizationService.IsAuthorizedAsync(
                securedRef,
                AuthorizationActions.View,
                subject,
                ct
            );
            if (!allowed)
            {
                continue;
            }

            authorized.Add(
                new SearchHitDto(
                    row.DocumentId,
                    row.ModuleName,
                    row.EntityTypeKey,
                    row.EntityId,
                    row.Title,
                    row.Subtitle,
                    SearchSnippetSanitizer.SanitizeHighlightedSnippet(row.RawHeadline),
                    row.UrlPath,
                    Math.Round(row.Score, 4, MidpointRounding.AwayFromZero),
                    row.IsPublic,
                    PostgreSqlFullTextSqlBuilder.SplitTags(row.TagsText),
                    PostgreSqlFullTextSqlBuilder.ParseFacets(row.FacetsJson)
                )
            );
            if (authorized.Count >= limit)
            {
                break;
            }
        }

        return authorized;
    }
}
