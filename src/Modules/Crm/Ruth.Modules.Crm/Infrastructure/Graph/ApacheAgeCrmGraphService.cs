using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Ruth.Modules.Crm.Contracts.Relationships;
using static Ruth.Modules.Crm.Infrastructure.Graph.ApacheAgeCypherHelper;

namespace Ruth.Modules.Crm.Infrastructure.Graph;

internal sealed partial class ApacheAgeCrmGraphService(
    [FromKeyedServices(typeof(CrmDbContext))] NpgsqlDataSource dataSource,
    ILogger<ApacheAgeCrmGraphService> logger
) : ICrmGraphService
{
    public async Task EnsureGraphInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var conn = await OpenAgeConnectionAsync(cancellationToken);
            await EnsureGraphExistsOnConnectionAsync(conn, cancellationToken);
        }
        catch (PostgresException ex)
        {
            LogAgeUnavailable(logger, ex);
        }
    }

    public async Task UpsertPersonVertexAsync(
        Guid personId,
        string fullName,
        CancellationToken cancellationToken = default
    )
    {
        var cypher =
            $"MERGE (p:Person {{entityId: '{FormatGuid(personId)}'}}) "
            + $"SET p.name = '{EscapeCypher(fullName)}' RETURN p.entityId";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task UpsertHouseholdVertexAsync(
        Guid householdId,
        string householdName,
        CancellationToken cancellationToken = default
    )
    {
        var cypher =
            $"MERGE (h:Household {{entityId: '{FormatGuid(householdId)}'}}) "
            + $"SET h.name = '{EscapeCypher(householdName)}' RETURN h.entityId";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task UpsertHouseholdMembershipEdgeAsync(
        Guid personId,
        string personName,
        Guid householdId,
        string householdName,
        string role,
        CancellationToken cancellationToken = default
    )
    {
        await UpsertPersonVertexAsync(personId, personName, cancellationToken);
        await UpsertHouseholdVertexAsync(householdId, householdName, cancellationToken);
        var cypher =
            $"MATCH (p:Person {{entityId: '{FormatGuid(personId)}'}}), "
            + $"(h:Household {{entityId: '{FormatGuid(householdId)}'}}) "
            + "MERGE (p)-[r:MEMBER_OF]->(h) "
            + $"SET r.detail = '{EscapeCypher(role)}' RETURN r";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task RemoveHouseholdMembershipEdgeAsync(
        Guid personId,
        Guid householdId,
        CancellationToken cancellationToken = default
    )
    {
        var cypher =
            $"MATCH (p:Person {{entityId: '{FormatGuid(personId)}'}})-[r:MEMBER_OF]->"
            + $"(h:Household {{entityId: '{FormatGuid(householdId)}'}}) DELETE r";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task UpsertKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        string ownerName,
        Guid relatedPersonId,
        string relatedName,
        string relationshipType,
        CancellationToken cancellationToken = default
    )
    {
        await UpsertPersonVertexAsync(ownerPersonId, ownerName, cancellationToken);
        await UpsertPersonVertexAsync(relatedPersonId, relatedName, cancellationToken);
        var edgeKey = EscapeCypher(relationshipType);
        var cypher =
            $"MATCH (a:Person {{entityId: '{FormatGuid(ownerPersonId)}'}}), "
            + $"(b:Person {{entityId: '{FormatGuid(relatedPersonId)}'}}) "
            + $"MERGE (a)-[r:KNOWN_RELATIONSHIP {{edgeKey: '{edgeKey}'}}]->(b) "
            + $"SET r.detail = '{edgeKey}' RETURN r";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task RemoveKnownRelationshipEdgeAsync(
        Guid ownerPersonId,
        Guid relatedPersonId,
        string relationshipType,
        CancellationToken cancellationToken = default
    )
    {
        var edgeKey = EscapeCypher(relationshipType);
        var cypher =
            $"MATCH (a:Person {{entityId: '{FormatGuid(ownerPersonId)}'}})"
            + $"-[r:KNOWN_RELATIONSHIP {{edgeKey: '{edgeKey}'}}]->"
            + $"(b:Person {{entityId: '{FormatGuid(relatedPersonId)}'}}) DELETE r";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task UpsertPeerNetworkEdgeAsync(
        Guid sourcePersonId,
        string sourceName,
        Guid targetPersonId,
        string targetName,
        string edgeKey,
        string detail,
        CancellationToken cancellationToken = default
    )
    {
        await UpsertPersonVertexAsync(sourcePersonId, sourceName, cancellationToken);
        await UpsertPersonVertexAsync(targetPersonId, targetName, cancellationToken);
        var cypher =
            $"MATCH (a:Person {{entityId: '{FormatGuid(sourcePersonId)}'}}), "
            + $"(b:Person {{entityId: '{FormatGuid(targetPersonId)}'}}) "
            + $"MERGE (a)-[r:PEER_NETWORK {{edgeKey: '{EscapeCypher(edgeKey)}'}}]->(b) "
            + $"SET r.detail = '{EscapeCypher(detail)}' RETURN r";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task RemovePersonVertexAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        var cypher = $"MATCH (p:Person {{entityId: '{FormatGuid(personId)}'}}) DETACH DELETE p";
        await ExecuteCypherNonQueryAsync(cypher, cancellationToken);
    }

    public async Task<PersonRelationshipGraphResponse> TraverseAsync(
        Guid personId,
        int maxHops,
        CancellationToken cancellationToken = default
    )
    {
        var clampedHops = Math.Clamp(maxHops, 1, 3);
        try
        {
            await using var conn = await OpenAgeConnectionAsync(cancellationToken);
            await EnsureGraphExistsOnConnectionAsync(conn, cancellationToken);

            var nodes = await QueryNodesAsync(conn, personId, clampedHops, cancellationToken);
            var edges =
                nodes.Count == 0 ? [] : await QueryEdgesAsync(conn, nodes, cancellationToken);

            return new PersonRelationshipGraphResponse(
                personId,
                clampedHops,
                PoweredByApacheAge: true,
                nodes,
                edges
            );
        }
        catch (PostgresException ex)
        {
            LogAgeUnavailable(logger, ex);
            return new PersonRelationshipGraphResponse(
                personId,
                clampedHops,
                PoweredByApacheAge: false,
                [],
                []
            );
        }
    }

    private static async Task<List<RelationshipGraphNodeResponse>> QueryNodesAsync(
        NpgsqlConnection conn,
        Guid personId,
        int clampedHops,
        CancellationToken cancellationToken
    )
    {
        var nodesById = new Dictionary<Guid, RelationshipGraphNodeResponse>();
        var rootSql = BuildCypherSql(
            $"MATCH (root:Person {{entityId: '{FormatGuid(personId)}'}}) "
                + "RETURN root.entityId, label(root), root.name",
            "id ag_catalog.agtype, lbl ag_catalog.agtype, nm ag_catalog.agtype",
            "id::text, lbl::text, nm::text"
        );

        await ReadNodeRowsAsync(conn, rootSql, hopOverride: 0, nodesById, cancellationToken);

        var hopCypher = string.Create(
            CultureInfo.InvariantCulture,
            $"MATCH path = (root:Person {{entityId: '{FormatGuid(personId)}'}})-[*1..{clampedHops}]-(n) RETURN n.entityId, label(n), n.name, length(path)"
        );
        var hopSql = BuildCypherSql(
            hopCypher,
            "id ag_catalog.agtype, lbl ag_catalog.agtype, nm ag_catalog.agtype, dist ag_catalog.agtype",
            "id::text, lbl::text, nm::text, dist::text"
        );

        await ReadNodeRowsAsync(conn, hopSql, hopOverride: null, nodesById, cancellationToken);
        return
        [
            .. nodesById
                .Values.OrderBy(static n => n.HopDistance)
                .ThenBy(static n => n.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    private static async Task ReadNodeRowsAsync(
        NpgsqlConnection conn,
        string sql,
        int? hopOverride,
        Dictionary<Guid, RelationshipGraphNodeResponse> nodesById,
        CancellationToken cancellationToken
    )
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var idStr = ReadAgString(reader, 0);
            if (!Guid.TryParse(idStr, out var entityId))
            {
                continue;
            }

            var label = ReadAgString(reader, 1) ?? "Node";
            var name = ReadAgString(reader, 2) ?? entityId.ToString("D");
            var hop = hopOverride ?? ReadAgInt(reader, 3);

            if (!nodesById.TryGetValue(entityId, out var existing) || hop < existing.HopDistance)
            {
                nodesById[entityId] = new RelationshipGraphNodeResponse(entityId, label, name, hop);
            }
        }
    }

    private static async Task<List<RelationshipGraphEdgeResponse>> QueryEdgesAsync(
        NpgsqlConnection conn,
        List<RelationshipGraphNodeResponse> nodes,
        CancellationToken cancellationToken
    )
    {
        var idList = string.Join(", ", nodes.Select(static n => $"'{FormatGuid(n.EntityId)}'"));
        var edgeSql = BuildCypherSql(
            $"MATCH (a)-[r]->(b) WHERE a.entityId IN [{idList}] AND b.entityId IN [{idList}] "
                + "RETURN a.entityId, b.entityId, label(r), r.detail",
            "src ag_catalog.agtype, dst ag_catalog.agtype, etype ag_catalog.agtype, detail ag_catalog.agtype",
            "src::text, dst::text, etype::text, detail::text"
        );

        var edges = new List<RelationshipGraphEdgeResponse>();
        var seen = new HashSet<(Guid, Guid, string, string)>();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = edgeSql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var srcStr = ReadAgString(reader, 0);
            var dstStr = ReadAgString(reader, 1);
            if (!Guid.TryParse(srcStr, out var src) || !Guid.TryParse(dstStr, out var dst))
            {
                continue;
            }

            var edgeType = ReadAgString(reader, 2) ?? "RELATED";
            var detail = ReadAgString(reader, 3) ?? string.Empty;
            if (seen.Add((src, dst, edgeType, detail)))
            {
                edges.Add(new RelationshipGraphEdgeResponse(src, dst, edgeType, detail));
            }
        }

        return edges;
    }

    private async Task ExecuteCypherNonQueryAsync(
        string cypher,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await using var conn = await OpenAgeConnectionAsync(cancellationToken);
            await EnsureGraphExistsOnConnectionAsync(conn, cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = BuildCypherSql(cypher, "v ag_catalog.agtype", "1");
            _ = await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex)
        {
            LogAgeOperationFailed(logger, ex);
        }
    }

    private async Task<NpgsqlConnection> OpenAgeConnectionAsync(CancellationToken cancellationToken)
    {
        var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var loadCmd = conn.CreateCommand();
        loadCmd.CommandText =
            "CREATE EXTENSION IF NOT EXISTS age; LOAD 'age'; SET search_path = ag_catalog, \"$user\", public;";
        _ = await loadCmd.ExecuteNonQueryAsync(cancellationToken);
        return conn;
    }

    private static async Task EnsureGraphExistsOnConnectionAsync(
        NpgsqlConnection conn,
        CancellationToken cancellationToken
    )
    {
        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM ag_catalog.ag_graph WHERE name = '{GraphName}');";
        var exists = (bool)(await checkCmd.ExecuteScalarAsync(cancellationToken) ?? false);
        if (!exists)
        {
            await using var createCmd = conn.CreateCommand();
            createCmd.CommandText = $"SELECT ag_catalog.create_graph('{GraphName}');";
            _ = await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    [LoggerMessage(
        EventId = 5101,
        Level = LogLevel.Warning,
        Message = "Apache AGE extension or crm_graph is unavailable on the current PostgreSQL server."
    )]
    private static partial void LogAgeUnavailable(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 5102,
        Level = LogLevel.Warning,
        Message = "Apache AGE graph synchronization command failed."
    )]
    private static partial void LogAgeOperationFailed(ILogger logger, Exception ex);
}
