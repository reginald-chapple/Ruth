using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Slices.Tags;

internal static class TagsSetsPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapTagsSetsPreferencesEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var tags = endpoints.MapGroup("/api/core/tags").WithTags("Tags").RequireAuthorization();
        MapTagRoutes(tags);

        var sets = endpoints
            .MapGroup("/api/core/entity-sets")
            .WithTags("Entity Sets")
            .RequireAuthorization();
        MapEntitySetRoutes(sets);

        var prefs = endpoints
            .MapGroup("/api/core/preferences")
            .WithTags("Person Preferences")
            .RequireAuthorization();
        MapPreferenceRoutes(prefs);

        return endpoints;
    }

    private static void MapTagRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", GetTagsAsync)
            .WithName("CoreGetTags")
            .WithSummary("Search personal and organizational tags")
            .Produces<IReadOnlyList<TagResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/entity/{entityTypeId:guid}/{entityId:guid}", GetEntityTagsAsync)
            .WithName("CoreGetEntityTags")
            .WithSummary("List tags applied to an entity")
            .Produces<IReadOnlyList<TagResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateTagAsync)
            .WithName("CoreCreateTag")
            .WithSummary("Create a personal or organizational tag")
            .Produces<TagResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPost("/assign", TagEntityAsync)
            .WithName("CoreTagEntity")
            .WithSummary("Apply a tag to an entity")
            .Produces(StatusCodes.Status204NoContent);

        _ = group
            .MapPost("/unassign", UntagEntityAsync)
            .WithName("CoreUntagEntity")
            .WithSummary("Remove a tag from an entity")
            .Produces(StatusCodes.Status204NoContent);

        _ = group
            .MapDelete("/{id:guid}", DeleteTagAsync)
            .WithName("CoreDeleteTag")
            .WithSummary("Delete a non-system tag")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapEntitySetRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", GetEntitySetsAsync)
            .WithName("CoreGetEntitySets")
            .WithSummary("List active or saved entity selection sets")
            .Produces<IReadOnlyList<EntitySetResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateEntitySetAsync)
            .WithName("CoreCreateEntitySet")
            .WithSummary("Create an ephemeral or saved entity selection set")
            .Produces<EntitySetResponse>(StatusCodes.Status201Created);

        _ = group
            .MapPost("/purge-expired", PurgeExpiredAsync)
            .WithName("CorePurgeExpiredEntitySets")
            .WithSummary("Purge expired ephemeral entity sets")
            .Produces<int>(StatusCodes.Status200OK);
    }

    private static void MapPreferenceRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/{ownerKey:guid}", GetPreferencesAsync)
            .WithName("CoreGetPersonPreferences")
            .WithSummary("List key-value preferences for a user/person")
            .Produces<IReadOnlyList<PersonPreferenceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/", SetPreferenceAsync)
            .WithName("CoreSetPersonPreference")
            .WithSummary("Upsert a user/person key-value preference")
            .Produces<PersonPreferenceResponse>(StatusCodes.Status200OK);

        _ = group
            .MapDelete("/{ownerKey:guid}/{key}", DeletePreferenceAsync)
            .WithName("CoreDeletePersonPreference")
            .WithSummary("Delete a user/person key-value preference")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TagResponse>>, ProblemHttpResult>
    > GetTagsAsync(
        Guid? entityTypeId,
        string? search,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var viewerId = TryResolveUserId(httpContext);
        var result = await dispatcher.QueryAsync<GetTagsQuery, IReadOnlyList<TagResponse>>(
            new GetTagsQuery(entityTypeId, viewerId, search, activeOnly ?? true),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TagResponse>>, ProblemHttpResult>
    > GetEntityTagsAsync(
        Guid entityTypeId,
        Guid entityId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var viewerId = TryResolveUserId(httpContext);
        var result = await dispatcher.QueryAsync<GetEntityTagsQuery, IReadOnlyList<TagResponse>>(
            new GetEntityTagsQuery(entityTypeId, entityId, viewerId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<TagResponse>, ProblemHttpResult>> CreateTagAsync(
        CreateTagCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateTagCommand, TagResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/tags/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> TagEntityAsync(
        TagEntityCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<TagEntityCommand, bool>(
            command with
            {
                RequestingUserId = command.RequestingUserId ?? userId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UntagEntityAsync(
        UntagEntityCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<UntagEntityCommand, bool>(
            command with
            {
                RequestingUserId = command.RequestingUserId ?? userId,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteTagAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var userId = TryResolveUserId(httpContext);
        var result = await dispatcher.SendAsync<DeleteTagCommand, bool>(
            new DeleteTagCommand(id, userId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EntitySetResponse>>, ProblemHttpResult>
    > GetEntitySetsAsync(
        Guid? entityTypeId,
        bool? includeExpired,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetEntitySetsQuery,
            IReadOnlyList<EntitySetResponse>
        >(new GetEntitySetsQuery(entityTypeId, includeExpired ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<EntitySetResponse>, ProblemHttpResult>
    > CreateEntitySetAsync(
        CreateEntitySetCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateEntitySetCommand, EntitySetResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/core/entity-sets/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<int>, ProblemHttpResult>> PurgeExpiredAsync(
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<PurgeExpiredEntitySetsCommand, int>(
            new PurgeExpiredEntitySetsCommand(),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<PersonPreferenceResponse>>, ProblemHttpResult>
    > GetPreferencesAsync(
        Guid ownerKey,
        string? keyPrefix,
        Guid? entityTypeId,
        Guid? entityId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPersonPreferencesQuery,
            IReadOnlyList<PersonPreferenceResponse>
        >(
            new GetPersonPreferencesQuery(ownerKey, keyPrefix, entityTypeId, entityId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PersonPreferenceResponse>, ProblemHttpResult>
    > SetPreferenceAsync(
        SetPersonPreferenceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetPersonPreferenceCommand,
            PersonPreferenceResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeletePreferenceAsync(
        Guid ownerKey,
        string key,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeletePersonPreferenceCommand, bool>(
            new DeletePersonPreferenceCommand(ownerKey, key),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CoreProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static Guid? TryResolveUserId(HttpContext context)
    {
        return Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? parsed
            : null;
    }
}
