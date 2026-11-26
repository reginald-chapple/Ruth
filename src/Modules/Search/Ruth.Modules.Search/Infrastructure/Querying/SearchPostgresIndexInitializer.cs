using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Search.Infrastructure.Querying;

/// <summary>
/// Ensures PostgreSQL pg_trgm extension, weighted stored tsvector column (A/B/C/D weights),
/// GIN full-text index, and GIN trigram indexes exist on search.search_documents.
/// </summary>
internal static class SearchPostgresIndexInitializer
{
    public static async Task EnsureFullTextIndexesAsync(SearchDbContext db, CancellationToken ct)
    {
        const string enableTrgmSql = "CREATE EXTENSION IF NOT EXISTS pg_trgm;";
        const string addSearchVectorSql =
            "ALTER TABLE search.search_documents "
            + "ADD COLUMN IF NOT EXISTS search_vector tsvector GENERATED ALWAYS AS ("
            + "setweight(to_tsvector('english', COALESCE(\"Title\", '')), 'A') || "
            + "setweight(to_tsvector('english', COALESCE(\"Subtitle\", '') || ' ' || COALESCE(\"TagsText\", '')), 'B') || "
            + "setweight(to_tsvector('english', COALESCE(\"Summary\", '')), 'C') || "
            + "setweight(to_tsvector('english', COALESCE(\"BodyText\", '')), 'D')"
            + ") STORED;";
        const string ftsIndexSql =
            "CREATE INDEX IF NOT EXISTS ix_search_documents_search_vector_gin "
            + "ON search.search_documents USING GIN (search_vector);";
        const string titleTrgmSql =
            "CREATE INDEX IF NOT EXISTS ix_search_documents_title_trgm_gin "
            + "ON search.search_documents USING GIN (\"Title\" gin_trgm_ops);";
        const string subtitleTrgmSql =
            "CREATE INDEX IF NOT EXISTS ix_search_documents_subtitle_trgm_gin "
            + "ON search.search_documents USING GIN (\"Subtitle\" gin_trgm_ops);";

        _ = await db.Database.ExecuteSqlRawAsync(enableTrgmSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(addSearchVectorSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(ftsIndexSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(titleTrgmSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(subtitleTrgmSql, ct);
    }
}
