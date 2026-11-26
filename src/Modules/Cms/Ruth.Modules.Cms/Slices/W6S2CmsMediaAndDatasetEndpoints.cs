using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S2CmsMediaAndDatasetEndpoints
{
    public static void Map(RouteGroupBuilder admin, IEndpointRouteBuilder endpoints)
    {
        MapMediaAccounts(admin);
        MapMediaFoldersAndElements(admin);
        MapBookmarks(admin);
        MapDatasetsAdmin(admin);
        MapDatasetsPublic(endpoints);
    }

    private static void MapMediaAccounts(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/media/accounts", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<GetMediaAccountsQuery, IReadOnlyList<MediaAccountResponse>>(
                        new GetMediaAccountsQuery(),
                        ct
                    )
                )
            )
            .WithName("GetMediaAccounts");

        _ = admin
            .MapPost(
                "/media/accounts",
                static async (
                    CreateMediaAccountCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateMediaAccountCommand, MediaAccountResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/media/accounts/{res.Value.Id:D}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateMediaAccount");

        _ = admin
            .MapPost(
                "/media/accounts/{accountId:guid}/sync",
                static async (Guid accountId, ICommandDispatcher c, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<SyncMediaAccountCommand, MediaAccountResponse>(
                            new SyncMediaAccountCommand(accountId),
                            ct
                        )
                    )
            )
            .WithName("SyncMediaAccount");
    }

    private static void MapMediaFoldersAndElements(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/media/folders",
                static async (Guid? mediaAccountId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetMediaFoldersQuery,
                            IReadOnlyList<MediaFolderResponse>
                        >(new GetMediaFoldersQuery(mediaAccountId), ct)
                    )
            )
            .WithName("GetMediaFolders");

        _ = admin
            .MapGet(
                "/media/elements",
                static async (
                    Guid? mediaFolderId,
                    Guid? mediaAccountId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetMediaElementsQuery,
                            IReadOnlyList<MediaElementResponse>
                        >(new GetMediaElementsQuery(mediaFolderId, mediaAccountId), ct)
                    )
            )
            .WithName("GetMediaElements");
    }

    private static void MapBookmarks(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/bookmarks/sections",
                static async (Guid? personId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetPersonalLinkSectionsQuery,
                            IReadOnlyList<PersonalLinkSectionResponse>
                        >(new GetPersonalLinkSectionsQuery(personId), ct)
                    )
            )
            .WithName("GetPersonalLinkSections");

        _ = admin
            .MapPost(
                "/bookmarks/sections",
                static async (
                    CreatePersonalLinkSectionCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreatePersonalLinkSectionCommand,
                        PersonalLinkSectionResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created("/api/cms/bookmarks/sections", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreatePersonalLinkSection");

        _ = admin
            .MapPost(
                "/bookmarks/sections/{sectionId:guid}/links",
                static async (
                    Guid sectionId,
                    AddPersonalLinkCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddPersonalLinkCommand, PersonalLinkSectionResponse>(
                            body with
                            {
                                SectionId = sectionId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddPersonalLink");
    }

    private static void MapDatasetsAdmin(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/datasets", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetPersistedDatasetsQuery,
                        IReadOnlyList<PersistedDatasetResponse>
                    >(new GetPersistedDatasetsQuery(), ct)
                )
            )
            .WithName("GetPersistedDatasets");

        _ = admin
            .MapPost(
                "/datasets",
                static async (
                    CreatePersistedDatasetCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreatePersistedDatasetCommand,
                        PersistedDatasetResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/datasets/{res.Value.AccessKey}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreatePersistedDataset");

        _ = admin
            .MapPost(
                "/datasets/{accessKey}/refresh",
                static async (string accessKey, ICommandDispatcher c, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<RefreshPersistedDatasetCommand, PersistedDatasetResponse>(
                            new RefreshPersistedDatasetCommand(accessKey),
                            ct
                        )
                    )
            )
            .WithName("RefreshPersistedDataset");
    }

    private static void MapDatasetsPublic(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGet(
                "/api/cms/datasets/{accessKey}",
                static async (string accessKey, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetPersistedDatasetByKeyQuery, PersistedDatasetResponse>(
                            new GetPersistedDatasetByKeyQuery(accessKey),
                            ct
                        )
                    )
            )
            .WithTags("CMS Feeds & Datasets")
            .AllowAnonymous()
            .WithName("GetPersistedDatasetByKey");
    }
}
