using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class CreateSignageApplicationCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateSignageApplicationCommand, SignageApplicationResponse>
{
    public async Task<Result<SignageApplicationResponse>> HandleAsync(
        CreateSignageApplicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = SignageApplication.Create(
            command.Name,
            command.Slug,
            command.Platform,
            command.SiteId is { } sid ? CmsSiteId.From(sid) : null,
            command.RefreshIntervalSeconds,
            command.ThemeId is { } tid ? CmsThemeId.From(tid) : null,
            command.DefaultContentChannelId is { } cid ? ContentChannelId.From(cid) : null,
            command.MediaFolderId is { } fid ? MediaFolderId.From(fid) : null,
            command.AdaptiveMessageKey,
            command.Slides
        );
        if (res.IsFailure)
        {
            return Result.Failure<SignageApplicationResponse>(res.PrimaryError);
        }

        var exists = await db
            .SignageApplications.AsNoTracking()
            .AnyAsync(a => a.Slug == res.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<SignageApplicationResponse>(
                CmsErrors.Conflict(
                    "SignageApplication.DuplicateSlug",
                    $"SignageApplication slug '{res.Value.Slug}' already exists."
                )
            );
        }

        _ = db.SignageApplications.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(res.Value));
    }
}
