using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Content;

internal sealed class CreateContentTopicCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateContentTopicCommand, ContentTopicResponse>
{
    public async Task<Result<ContentTopicResponse>> HandleAsync(
        CreateContentTopicCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = ContentTopic.Create(
            command.Name,
            command.Slug,
            command.Description,
            command.ParentTopicId is { } pid ? ContentTopicId.From(pid) : null,
            command.Order
        );
        if (res.IsFailure)
        {
            return Result.Failure<ContentTopicResponse>(res.PrimaryError);
        }

        var exists = await db
            .ContentTopics.AsNoTracking()
            .AnyAsync(t => t.Slug == res.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<ContentTopicResponse>(
                CmsErrors.Conflict(
                    "ContentTopic.DuplicateSlug",
                    $"ContentTopic slug '{res.Value.Slug}' already exists."
                )
            );
        }

        _ = db.ContentTopics.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }
}
