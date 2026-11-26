using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateCmsLayoutCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateCmsLayoutCommand, CmsLayoutResponse>
{
    public async Task<Result<CmsLayoutResponse>> HandleAsync(
        CreateCmsLayoutCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(command.SiteId);
        var siteExists = await db
            .Sites.AsNoTracking()
            .AnyAsync(s => s.Id == siteId, cancellationToken);
        if (!siteExists)
        {
            return Result.Failure<CmsLayoutResponse>(
                CmsErrors.NotFound("Site", command.SiteId.ToString("D"))
            );
        }

        var layoutRes = CmsLayout.Create(
            siteId,
            command.Name,
            command.Slug,
            command.FileName,
            command.Description,
            command.Zones
        );
        if (layoutRes.IsFailure)
        {
            return Result.Failure<CmsLayoutResponse>(layoutRes.PrimaryError);
        }

        var exists = await db
            .Layouts.AsNoTracking()
            .AnyAsync(l => l.SiteId == siteId && l.Slug == layoutRes.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<CmsLayoutResponse>(
                CmsErrors.Conflict(
                    "Layout.DuplicateSlug",
                    $"Layout slug '{layoutRes.Value.Slug}' already exists on this site."
                )
            );
        }

        _ = db.Layouts.Add(layoutRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(layoutRes.Value));
    }
}
