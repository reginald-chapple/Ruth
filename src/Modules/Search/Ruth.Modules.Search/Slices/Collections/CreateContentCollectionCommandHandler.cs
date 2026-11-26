using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Domain;
using Ruth.Modules.Search.Domain.Collections;
using Ruth.Modules.Search.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Search.Slices.Collections;

internal sealed class CreateContentCollectionCommandHandler(
    SearchDbContext db,
    IEnumerable<ISearchDocumentProvider> providers
) : ICommandHandler<CreateContentCollectionCommand, ContentCollectionResponse>
{
    public async Task<Result<ContentCollectionResponse>> HandleAsync(
        CreateContentCollectionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var slug = SearchTextNormalizer.NormalizeSlug(command.CollectionKey);
        if (
            await db
                .ContentCollections.AsNoTracking()
                .AnyAsync(c => c.CollectionKey == slug, cancellationToken)
        )
        {
            return Result.Failure<ContentCollectionResponse>(
                SearchErrors.Conflict(
                    "Collection.DuplicateKey",
                    $"ContentCollection key '{slug}' already exists."
                )
            );
        }

        var created = ContentCollectionDefinition.Create(
            command.Name,
            command.CollectionKey,
            command.Description,
            command.IsPublic,
            command.EnableRequestFilters,
            command.EnableSegments
        );
        if (created.IsFailure)
        {
            return Result.Failure<ContentCollectionResponse>(created.PrimaryError);
        }

        var providerMap = providers.ToDictionary(
            static p => p.ProviderKey,
            StringComparer.OrdinalIgnoreCase
        );
        if (command.Sources is { Count: > 0 })
        {
            foreach (var src in command.Sources)
            {
                var entityKey = providerMap.TryGetValue(src.ProviderKey, out var provider)
                    ? provider.EntityTypeKey
                    : src.ProviderKey;
                var addRes = created.Value.AddSource(
                    src.ProviderKey,
                    entityKey,
                    src.OccurrencesToShow,
                    src.CustomBoostWeight,
                    src.FilterConfigJson
                );
                if (addRes.IsFailure)
                {
                    return Result.Failure<ContentCollectionResponse>(addRes.PrimaryError);
                }
            }
        }

        _ = db.ContentCollections.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(SearchDtoMapper.ToResponse(created.Value));
    }
}
