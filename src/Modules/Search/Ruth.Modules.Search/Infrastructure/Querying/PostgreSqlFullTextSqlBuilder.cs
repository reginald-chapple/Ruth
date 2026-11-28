using System.Data.Common;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Querying;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Search.Infrastructure.Querying;

internal static class PostgreSqlFullTextSqlBuilder
{
    public static string BuildFullTextSql()
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
            + "WHERE d.tenant_id = @tenantId "
            + "AND (@collectionId IS NULL OR d.\"ContentCollectionId\" = @collectionId) "
            + "AND (@publicOnly = FALSE OR d.\"IsPublic\" = TRUE) "
            + "AND (@modules IS NULL OR d.\"ModuleName\" = ANY(@modules)) "
            + "AND (@entityTypes IS NULL OR d.\"EntityTypeKey\" = ANY(@entityTypes)) "
            + "AND (d.search_vector @@ q.tsq OR similarity(d.\"Title\", @query) >= 0.2 OR similarity(COALESCE(d.\"Subtitle\", ''), @query) >= 0.25 "
            + "OR d.\"Title\" ILIKE @likeQuery OR COALESCE(d.\"Subtitle\", '') ILIKE @likeQuery) "
            + "ORDER BY score DESC, d.\"IndexedDateTimeUtc\" DESC LIMIT @limit";
    }

    public static string BuildBrowseSql()
    {
        return "SELECT d.\"Id\", d.\"ModuleName\", d.\"EntityTypeKey\", d.\"EntityId\", d.\"Title\", d.\"Subtitle\", "
            + "COALESCE(NULLIF(d.\"Summary\", ''), d.\"Title\") AS headline, "
            + "d.\"UrlPath\", 1.0::double precision AS score, "
            + "d.\"IsPublic\", d.\"TagsText\", d.\"FacetsJson\"::text, d.\"SecurityEntityTypeKey\", d.\"SecurityEntityId\" "
            + "FROM search.search_documents d "
            + "WHERE d.tenant_id = @tenantId "
            + "AND (@collectionId IS NULL OR d.\"ContentCollectionId\" = @collectionId) "
            + "AND (@publicOnly = FALSE OR d.\"IsPublic\" = TRUE) "
            + "AND (@modules IS NULL OR d.\"ModuleName\" = ANY(@modules)) "
            + "AND (@entityTypes IS NULL OR d.\"EntityTypeKey\" = ANY(@entityTypes)) "
            + "ORDER BY d.\"IndexedDateTimeUtc\" DESC LIMIT @limit";
    }

    public static void AddFilterParameters(
        DbCommand cmd,
        TenantId tenantId,
        string rawText,
        ContentCollectionId? collectionId,
        IReadOnlyList<string>? moduleNames,
        IReadOnlyList<string>? entityTypeKeys,
        bool publicOnly,
        int overReadLimit
    )
    {
        _ = cmd.Parameters.Add(
            new NpgsqlParameter("tenantId", tenantId.Value) { NpgsqlDbType = NpgsqlDbType.Uuid }
        );
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

    public static SearchResultsResponse BuildResponse(
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

    public static string[] SplitTags(string tagsText)
    {
        return string.IsNullOrWhiteSpace(tagsText)
            ? []
            : tagsText.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }

    public static Dictionary<string, string> ParseFacets(string facetsJson)
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
