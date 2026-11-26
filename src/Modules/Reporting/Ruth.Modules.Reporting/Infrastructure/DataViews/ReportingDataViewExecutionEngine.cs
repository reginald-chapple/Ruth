using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.DataViews;

internal sealed class ReportingDataViewExecutionEngine(
    ReportingDbContext db,
    IEnumerable<IDataViewSourceProvider> sourceProviders
)
{
    private static readonly Dictionary<
        string,
        IReadOnlyDictionary<string, string>
    > _allowlistedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ruth.Crm.Person"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "first_name",
            ["LastName"] = "last_name",
            ["NickName"] = "nick_name",
            ["Email"] = "email",
            ["RecordStatus"] = "record_status",
            ["ConnectionStatus"] = "connection_status",
            ["PrimaryBranchId"] = "primary_branch_id",
            ["IsDeceased"] = "is_deceased",
            ["CreatedOnUtc"] = "created_on_utc",
        },
        ["Ruth.Groups.Group"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "name",
            ["IsActive"] = "is_active",
            ["BranchId"] = "branch_id",
            ["GroupCapacity"] = "group_capacity",
            ["CreatedOnUtc"] = "created_on_utc",
        },
        ["Ruth.Finance.FinancialTransaction"] = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["CurrencyCode"] = "currency_code",
            ["Status"] = "status",
            ["BranchId"] = "branch_id",
            ["TransactionDateTimeUtc"] = "transaction_date_time_utc",
        },
    };

    public async Task<(
        IReadOnlyList<DataViewEntityRecord> MatchedRecords,
        string CompiledSqlPreview
    )> EvaluateAsync(DataView dataView, int maxRows, CancellationToken ct)
    {
        var rootFilter = dataView.BuildFilterTree();
        var colMap = _allowlistedColumns.TryGetValue(dataView.EntityTypeKey, out var map)
            ? map
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = "name",
                ["Status"] = "status",
                ["IsActive"] = "is_active",
            };
        var compiled = DataViewSqlCompiler.Compile(rootFilter, colMap);

        var candidates = await LoadCandidateRecordsAsync(dataView.EntityTypeKey, maxRows * 4, ct);
        var crossSets = await ResolveCrossModuleSetsAsync(rootFilter, ct);

        List<DataViewEntityRecord> matched = [];
        foreach (var candidate in candidates)
        {
            if (DataViewAstEvaluator.Evaluate(rootFilter, candidate, crossSets))
            {
                matched.Add(candidate);
                if (matched.Count >= maxRows)
                {
                    break;
                }
            }
        }

        return (matched, compiled.WhereClause);
    }

    public Task<IReadOnlyList<DataViewEntityRecord>> LoadCandidateRecordsAsync(
        string entityTypeKey,
        int maxRows,
        CancellationToken ct
    )
    {
        var customProvider = sourceProviders.FirstOrDefault(p =>
            string.Equals(p.EntityTypeKey, entityTypeKey, StringComparison.OrdinalIgnoreCase)
        );
        return customProvider is not null
            ? customProvider.LoadRecordsAsync(maxRows, ct)
            : entityTypeKey.ToLowerInvariant() switch
            {
                "ruth.crm.person" => LoadPeopleFromPostgresAsync(maxRows, ct),
                "ruth.groups.group" => LoadGroupsFromPostgresAsync(maxRows, ct),
                "ruth.finance.financialtransaction" => LoadTransactionsFromPostgresAsync(
                    maxRows,
                    ct
                ),
                _ => Task.FromResult<IReadOnlyList<DataViewEntityRecord>>([]),
            };
    }

    private async Task<Dictionary<string, IReadOnlySet<Guid>>> ResolveCrossModuleSetsAsync(
        DataViewFilterNodeDto node,
        CancellationToken ct
    )
    {
        var sets = new Dictionary<string, IReadOnlySet<Guid>>(StringComparer.OrdinalIgnoreCase);
        List<DataViewFilterNodeDto> setRules = [];
        CollectCrossModuleRules(node, setRules);

        foreach (var rule in setRules)
        {
            var lookupKey = DataViewAstEvaluator.BuildPersonSetLookupKey(rule.FieldKey, rule.Value);
            if (!sets.ContainsKey(lookupKey))
            {
                sets[lookupKey] = await LoadPersonSetAsync(rule.FieldKey, rule.Value, ct);
            }
        }

        return sets;
    }

    private static void CollectCrossModuleRules(
        DataViewFilterNodeDto node,
        List<DataViewFilterNodeDto> target
    )
    {
        if (
            node.ExpressionType == DataViewFilterExpressionType.Rule
            && node.RuleKind == DataViewFilterRuleKind.CrossModulePersonSet
        )
        {
            target.Add(node);
            return;
        }

        if (node.Children is null)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            CollectCrossModuleRules(child, target);
        }
    }

    private async Task<IReadOnlySet<Guid>> LoadPersonSetAsync(
        string? sourceKind,
        string? targetValue,
        CancellationToken ct
    )
    {
        var kind = string.IsNullOrWhiteSpace(sourceKind)
            ? "dataview"
            : sourceKind.Trim().ToLowerInvariant();
        if (
            string.Equals(kind, "dataview", StringComparison.Ordinal)
            && Guid.TryParse(targetValue, out var dvGuid)
        )
        {
            var dvId = DataViewId.From(dvGuid);
            var ids = await db
                .DataViewPersistedValues.AsNoTracking()
                .Where(v => v.DataViewId == dvId)
                .Select(static v => v.EntityId)
                .ToListAsync(ct);
            return ids.ToHashSet();
        }

        return string.Equals(kind, "group", StringComparison.Ordinal)
            && Guid.TryParse(targetValue, out var groupId)
                ? await QueryGuidColumnIfTableExistsAsync(
                    "groups.group_members",
                    "SELECT \"PersonId\" FROM groups.group_members WHERE \"GroupId\" = @p0 AND \"Status\" = 'Active'",
                    parameter: groupId,
                    ct
                )
            : string.Equals(kind, "finance", StringComparison.Ordinal)
                ? await QueryGuidColumnIfTableExistsAsync(
                    "finance.financial_transactions",
                    "SELECT DISTINCT \"AuthorizedPersonId\" FROM finance.financial_transactions WHERE \"AuthorizedPersonId\" IS NOT NULL",
                    parameter: null,
                    ct
                )
            : string.Equals(kind, "attendance", StringComparison.Ordinal)
                ? await QueryGuidColumnIfTableExistsAsync(
                    "groups.attendances",
                    "SELECT DISTINCT \"PersonId\" FROM groups.attendances WHERE \"DidAttend\" = TRUE",
                    parameter: null,
                    ct
                )
            : new HashSet<Guid>();
    }

    private async Task<IReadOnlyList<DataViewEntityRecord>> LoadPeopleFromPostgresAsync(
        int maxRows,
        CancellationToken ct
    )
    {
        if (!await TableExistsAsync("crm.people", ct))
        {
            return [];
        }

        const string sql =
            "SELECT \"Id\", \"FirstName\", \"LastName\", \"NickName\", \"Email\", \"RecordStatus\", \"ConnectionStatus\", "
            + "\"PrimaryBranchId\", \"IsDeceased\", \"CreatedOnUtc\", attributes::text "
            + "FROM crm.people ORDER BY \"CreatedOnUtc\" DESC LIMIT @limit";

        return await ExecuteRecordsQueryAsync(
            sql,
            maxRows,
            static reader =>
            {
                var id = reader.GetGuid(0);
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["FirstName"] = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ["LastName"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ["NickName"] = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ["Email"] = reader.IsDBNull(4) ? null : reader.GetString(4),
                    ["RecordStatus"] = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ["ConnectionStatus"] = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ["PrimaryBranchId"] = reader.IsDBNull(7) ? null : reader.GetGuid(7),
                    ["IsDeceased"] = !reader.IsDBNull(8) && reader.GetBoolean(8),
                    ["CreatedOnUtc"] = reader.IsDBNull(9)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(9),
                };
                var attrs = ParseJsonbDictionary(reader.IsDBNull(10) ? null : reader.GetString(10));
                return new DataViewEntityRecord(id, "Ruth.Crm.Person", id, values, attrs);
            },
            ct
        );
    }

    private async Task<IReadOnlyList<DataViewEntityRecord>> LoadGroupsFromPostgresAsync(
        int maxRows,
        CancellationToken ct
    )
    {
        if (!await TableExistsAsync("groups.groups", ct))
        {
            return [];
        }

        const string sql =
            "SELECT \"Id\", \"Name\", \"IsActive\", \"BranchId\", \"GroupCapacity\", \"CreatedOnUtc\", attributes::text "
            + "FROM groups.groups ORDER BY \"Name\" LIMIT @limit";

        return await ExecuteRecordsQueryAsync(
            sql,
            maxRows,
            static reader =>
            {
                var id = reader.GetGuid(0);
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Name"] = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ["IsActive"] = !reader.IsDBNull(2) && reader.GetBoolean(2),
                    ["BranchId"] = reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    ["GroupCapacity"] = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    ["CreatedOnUtc"] = reader.IsDBNull(5)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(5),
                };
                var attrs = ParseJsonbDictionary(reader.IsDBNull(6) ? null : reader.GetString(6));
                return new DataViewEntityRecord(
                    id,
                    "Ruth.Groups.Group",
                    PersonId: null,
                    values,
                    attrs
                );
            },
            ct
        );
    }

    private async Task<IReadOnlyList<DataViewEntityRecord>> LoadTransactionsFromPostgresAsync(
        int maxRows,
        CancellationToken ct
    )
    {
        if (!await TableExistsAsync("finance.financial_transactions", ct))
        {
            return [];
        }

        const string sql =
            "SELECT \"Id\", \"AuthorizedPersonId\", \"CurrencyCode\", \"Status\", NULL::uuid, \"TransactionDateTimeUtc\" "
            + "FROM finance.financial_transactions ORDER BY \"TransactionDateTimeUtc\" DESC LIMIT @limit";

        return await ExecuteRecordsQueryAsync(
            sql,
            maxRows,
            static reader =>
            {
                var id = reader.GetGuid(0);
                Guid? personId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PersonId"] = personId,
                    ["CurrencyCode"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ["Status"] = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ["BranchId"] = reader.IsDBNull(4) ? null : reader.GetGuid(4),
                    ["TransactionDateTimeUtc"] = reader.IsDBNull(5)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(5),
                };
                return new DataViewEntityRecord(
                    id,
                    "Ruth.Finance.FinancialTransaction",
                    personId,
                    values
                );
            },
            ct
        );
    }

    private async Task<IReadOnlyList<DataViewEntityRecord>> ExecuteRecordsQueryAsync(
        string sql,
        int maxRows,
        Func<DbDataReader, DataViewEntityRecord> mapRow,
        CancellationToken ct
    )
    {
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
            _ = cmd.Parameters.Add(new NpgsqlParameter("limit", Math.Clamp(maxRows, 1, 5000)));

            List<DataViewEntityRecord> results = [];
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(mapRow(reader));
            }

            return results;
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlySet<Guid>> QueryGuidColumnIfTableExistsAsync(
        string tableName,
        string sql,
        Guid? parameter,
        CancellationToken ct
    )
    {
        if (!await TableExistsAsync(tableName, ct))
        {
            return new HashSet<Guid>();
        }

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
            if (parameter.HasValue)
            {
                _ = cmd.Parameters.Add(new NpgsqlParameter("p0", parameter.Value));
            }

            var set = new HashSet<Guid>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (!await reader.IsDBNullAsync(0, ct))
                {
                    _ = set.Add(reader.GetGuid(0));
                }
            }

            return set;
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }
    }

    internal async Task<bool> TableExistsAsync(string qualifiedTableName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await conn.OpenAsync(ct);
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT to_regclass(@name) IS NOT NULL";
            _ = cmd.Parameters.Add(new NpgsqlParameter("name", qualifiedTableName));
            var scalar = await cmd.ExecuteScalarAsync(ct);
            return scalar is bool b && b;
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }
    }

    private static Dictionary<string, string?>? ParseJsonbDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            return parsed is null
                ? null
                : new Dictionary<string, string?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
