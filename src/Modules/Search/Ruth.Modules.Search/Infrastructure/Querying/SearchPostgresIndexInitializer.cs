using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Search.Infrastructure.Querying;

/// <summary>
/// Ensures PostgreSQL pg_trgm extension, weighted stored tsvector column (A/B/C/D weights),
/// GIN full-text index, and GIN trigram indexes exist on search.search_documents.
/// </summary>
internal static class SearchPostgresIndexInitializer
{
    private static readonly string[] _ddlStatements =
    [
        "CREATE EXTENSION IF NOT EXISTS pg_trgm;",
        "ALTER TABLE search.search_documents "
            + "ADD COLUMN IF NOT EXISTS search_vector tsvector GENERATED ALWAYS AS ("
            + "setweight(to_tsvector('english', COALESCE(\"Title\", '')), 'A') || "
            + "setweight(to_tsvector('english', COALESCE(\"Subtitle\", '') || ' ' || COALESCE(\"TagsText\", '')), 'B') || "
            + "setweight(to_tsvector('english', COALESCE(\"Summary\", '')), 'C') || "
            + "setweight(to_tsvector('english', COALESCE(\"BodyText\", '')), 'D')"
            + ") STORED;",
        "CREATE INDEX IF NOT EXISTS ix_search_documents_tenant_indexed_utc "
            + "ON search.search_documents (tenant_id, \"IndexedDateTimeUtc\" DESC);",
        "CREATE INDEX IF NOT EXISTS ix_search_documents_search_vector_gin "
            + "ON search.search_documents USING GIN (search_vector);",
        "CREATE INDEX IF NOT EXISTS ix_search_documents_title_trgm_gin "
            + "ON search.search_documents USING GIN (\"Title\" gin_trgm_ops);",
        "CREATE INDEX IF NOT EXISTS ix_search_documents_subtitle_trgm_gin "
            + "ON search.search_documents USING GIN (\"Subtitle\" gin_trgm_ops);",
    ];

    public static async Task EnsureFullTextIndexesAsync(
        PostgresRlsRoleProvisioner? provisioner,
        SearchDbContext? fallbackDb,
        CancellationToken ct
    )
    {
        if (provisioner is not null)
        {
            await using var conn = new NpgsqlConnection(provisioner.AdminConnectionString);
            await conn.OpenAsync(ct);
            foreach (var sql in _ddlStatements)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                _ = await cmd.ExecuteNonQueryAsync(ct);
            }

            await provisioner.EnsureAppRoleAndSchemaGrantsAsync("search", ct);
            return;
        }

        if (fallbackDb is not null)
        {
            foreach (var sql in _ddlStatements)
            {
                _ = await fallbackDb.Database.ExecuteSqlRawAsync(sql, ct);
            }
        }
    }
}
