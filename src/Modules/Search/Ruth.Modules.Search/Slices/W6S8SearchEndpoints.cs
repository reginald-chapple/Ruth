using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Slices;

internal static class W6S8SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints
            .MapGroup("/api/search")
            .WithTags("Universal Search")
            .RequireAuthorization();
        MapCollections(admin);
        MapIndexing(admin);
        MapSpotlight(admin);
        MapPublicSearch(endpoints);
        return endpoints;
    }

    private static void MapCollections(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/collections",
                static async (bool? includeInactive, IQueryDispatcher q, CancellationToken ct) =>
                    SearchHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetContentCollectionsQuery,
                            IReadOnlyList<ContentCollectionResponse>
                        >(new GetContentCollectionsQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetSearchContentCollections");

        _ = group
            .MapPost(
                "/collections",
                static async (
                    CreateContentCollectionCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateContentCollectionCommand,
                        ContentCollectionResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/search/collections/{res.Value.Id:D}", res.Value)
                        : SearchHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateSearchContentCollection");
    }

    private static void MapIndexing(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/documents",
                static async (
                    UpsertSearchDocumentsCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    SearchHttpResultMapper.ToHttpResult(
                        await c.SendAsync<UpsertSearchDocumentsCommand, SearchIndexRebuildResponse>(
                            cmd,
                            ct
                        )
                    )
            )
            .WithName("UpsertSearchDocuments");

        _ = group
            .MapPost(
                "/reindex",
                static async (
                    RebuildSearchIndexCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    SearchHttpResultMapper.ToHttpResult(
                        await c.SendAsync<RebuildSearchIndexCommand, SearchIndexRebuildResponse>(
                            cmd,
                            ct
                        )
                    )
            )
            .WithName("RebuildSearchIndex");
    }

    private static void MapSpotlight(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/spotlight",
                static async (
                    string? q,
                    string? collection,
                    string? module,
                    string? entityType,
                    int? limit,
                    HttpContext http,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                {
                    var subject = BuildSubjectFromPrincipal(http.User);
                    IReadOnlyList<string>? modules = SplitCsv(module);
                    IReadOnlyList<string>? entityTypes = SplitCsv(entityType);
                    return SearchHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            ExecuteUniversalSearchQuery,
                            SearchResultsResponse
                        >(
                            new ExecuteUniversalSearchQuery(
                                q ?? string.Empty,
                                subject,
                                collection,
                                modules,
                                entityTypes,
                                PublicOnly: false,
                                Limit: limit ?? 20
                            ),
                            ct
                        )
                    );
                }
            )
            .WithName("SearchSpotlight");
    }

    private static void MapPublicSearch(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGet(
                "/api/search/public",
                static async (
                    string? q,
                    string? collection,
                    string? module,
                    int? limit,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    SearchHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            ExecuteUniversalSearchQuery,
                            SearchResultsResponse
                        >(
                            new ExecuteUniversalSearchQuery(
                                q ?? string.Empty,
                                SecuritySubject.Anonymous,
                                collection,
                                SplitCsv(module),
                                EntityTypeKeys: null,
                                PublicOnly: true,
                                Limit: limit ?? 20
                            ),
                            ct
                        )
                    )
            )
            .WithTags("Public Content Search")
            .WithName("SearchPublicContent")
            .AllowAnonymous();
    }

    private static string[]? SplitCsv(string? csv)
    {
        return string.IsNullOrWhiteSpace(csv)
            ? null
            : csv.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
    }

    private static SecuritySubject BuildSubjectFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return SecuritySubject.Anonymous;
        }

        var subClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(subClaim, out var parsed) ? parsed : null;
        HashSet<Guid> roleIds =
        [
            .. principal
                .FindAll("role_id")
                .Select(static c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(static g => g != Guid.Empty),
        ];
        return new SecuritySubject(userId, roleIds);
    }
}
