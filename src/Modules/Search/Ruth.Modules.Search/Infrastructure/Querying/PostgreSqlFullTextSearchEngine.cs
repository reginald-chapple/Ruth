using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Querying;
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

        return BuildResponse(rawText, authorized, (int)sw.ElapsedMilliseconds);
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
        var sql = hasQuery ? BuildFullTextSql() : BuildBrowseSql();
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await conn.OpenAsync(ct);
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            AddFilterParameters(
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
                await conn.CloseAsync();
            }
        }
    }

    private static string BuildFullTextSql()
    {
        return "WITH q AS (SELECT websearch_to_tsquery('english', @query) AS tsq) "
            + "SELECT d.\"Id\", d.\"ModuleName\", d.\"EntityTypeKey\", d.\"EntityId\", d.\"Title\", d.\"Subtitle\", "
            + "ts_headline('english', COALESCE(NULLIF(d.\"Summary\", ''), NULLIF(d.\"BodyText\", ''), d.\"Title\"), q.tsq, "
            + "'StartSel="
            + SearchSnippetSanitizer.StartMarker
            + ", StopSel="
            + SearchSnippetSanitizer.StopMarker
            + ", MaxFragments=2, MaxWords=24, MinWords=6') AS headline, "
            + "d.\"UrlPath\", "
            + "(COALESCE(ts_rank_cd(d.search_vector, q.tsq, 32), 0)::double precision * 2.0 + "
            + "GREATEST(similarity(d.\"Title\", @query), similarity(COALESCE(d.\"Subtitle\", ''), @query))::double precision) AS score, "
            + "d.\"IsPublic\", d.\"TagsText\", d.\"FacetsJson\"::text, d.\"SecurityEntityTypeKey\", d.\"SecurityEntityId\" "
            + "FROM search.search_documents d, q "
            + "WHERE (@collectionId IS NULL OR d.\"ContentCollectionId\" = @collectionId) "
            + "AND (@publicOnly = FALSE OR d.\"IsPublic\" = TRUE) "
            + "AND (@modules IS NULL OR d.\"ModuleName\" = ANY(@modules)) "
            + "AND (@entityTypes IS NULL OR d.\"EntityTypeKey\" = ANY(@entityTypes)) "
            + "AND (d.search_vector @@ q.tsq OR similarity(d.\"Title\", @query) >= 0.2 OR similarity(COALESCE(d.\"Subtitle\", ''), @query) >= 0.25 "
            + "OR d.\"Title\" ILIKE @likeQuery OR COALESCE(d.\"Subtitle\", '') ILIKE @likeQuery) "
            + "ORDER BY score DESC, d.\"IndexedDateTimeUtc\" DESC LIMIT @limit";
    }

    private static string BuildBrowseSql()
    {
        return "SELECT d.\"Id\", d.\"ModuleName\", d.\"EntityTypeKey\", d.\"EntityId\", d.\"Title\", d.\"Subtitle\", "
            + "COALESCE(NULLIF(d.\"Summary\", ''), d.\"Title\") AS headline, "
            + "d.\"UrlPath\", 1.0::double precision AS score, "
            + "d.\"IsPublic\", d.\"TagsText\", d.\"FacetsJson\"::text, d.\"SecurityEntityTypeKey\", d.\"SecurityEntityId\" "
            + "FROM search.search_documents d "
            + "WHERE (@collectionId IS NULL OR d.\"ContentCollectionId\" = @collectionId) "
            + "AND (@publicOnly = FALSE OR d.\"IsPublic\" = TRUE) "
            + "AND (@modules IS NULL OR d.\"ModuleName\" = ANY(@modules)) "
            + "AND (@entityTypes IS NULL OR d.\"EntityTypeKey\" = ANY(@entityTypes)) "
            + "ORDER BY d.\"IndexedDateTimeUtc\" DESC LIMIT @limit";
    }

    private static void AddFilterParameters(
        System.Data.Common.DbCommand cmd,
        string rawText,
        ContentCollectionId? collectionId,
        IReadOnlyList<string>? moduleNames,
        IReadOnlyList<string>? entityTypeKeys,
        bool publicOnly,
        int overReadLimit
    )
    {
        _ = cmd.Parameters.Add(new NpgsqlParameter("query", rawText));
        _ = cmd.Parameters.Add(new NpgsqlParameter("likeQuery", $"%{rawText}%"));
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("collectionId", (object?)collectionId?.Value ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Uuid,
            }
        );
        _ = cmd.Parameters.Add(new NpgsqlParameter("publicOnly", publicOnly));
        var modulesArr = moduleNames is { Count: > 0 } ? moduleNames.ToArray() : null;
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("modules", (object?)modulesArr ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text,
            }
        );
        var entityArr = entityTypeKeys is { Count: > 0 } ? entityTypeKeys.ToArray() : null;
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("entityTypes", (object?)entityArr ?? DBNull.Value)
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text,
            }
        );
        _ = cmd.Parameters.Add(new NpgsqlParameter("limit", overReadLimit));
    }

    private static async Task<List<CandidateRow>> ReadCandidatesAsync(
        System.Data.Common.DbCommand cmd,
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
                    SplitTags(row.TagsText),
                    ParseFacets(row.FacetsJson)
                )
            );
            if (authorized.Count >= limit)
            {
                break;
            }
        }

        return authorized;
    }

    private static SearchResultsResponse BuildResponse(
        string queryText,
        List<SearchHitDto> hits,
        int elapsedMs
    )
    {
        List<SearchFacetBucketDto> moduleFacets =
        [
            .. hits.GroupBy(static h => h.ModuleName, StringComparer.OrdinalIgnoreCase)
                .Select(static g => new SearchFacetBucketDto(g.Key, g.Count()))
                .OrderByDescending(static b => b.Count)
                .ThenBy(static b => b.Value, StringComparer.OrdinalIgnoreCase),
        ];
        List<SearchFacetBucketDto> entityFacets =
        [
            .. hits.GroupBy(static h => h.EntityTypeKey, StringComparer.OrdinalIgnoreCase)
                .Select(static g => new SearchFacetBucketDto(g.Key, g.Count()))
                .OrderByDescending(static b => b.Count)
                .ThenBy(static b => b.Value, StringComparer.OrdinalIgnoreCase),
        ];
        return new SearchResultsResponse(
            queryText,
            hits.Count,
            elapsedMs,
            hits,
            moduleFacets,
            entityFacets
        );
    }

    private static string[] SplitTags(string tagsText)
    {
        return string.IsNullOrWhiteSpace(tagsText)
            ? []
            : tagsText.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }

    private static Dictionary<string, string> ParseFacets(string facetsJson)
    {
        if (string.IsNullOrWhiteSpace(facetsJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(facetsJson);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
