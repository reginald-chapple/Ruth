using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateCmsPageCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateCmsPageCommand, CmsPageResponse>
{
    public async Task<Result<CmsPageResponse>> HandleAsync(
        CreateCmsPageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(command.SiteId);
        var layoutId = CmsLayoutId.From(command.LayoutId);

        var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
        if (site is null)
        {
            return Result.Failure<CmsPageResponse>(
                CmsErrors.NotFound("Site", command.SiteId.ToString("D"))
            );
        }

        var layoutExists = await db
            .Layouts.AsNoTracking()
            .AnyAsync(l => l.Id == layoutId && l.SiteId == siteId, cancellationToken);
        if (!layoutExists)
        {
            return Result.Failure<CmsPageResponse>(
                CmsErrors.NotFound("Layout", command.LayoutId.ToString("D"))
            );
        }

        var pageRes = CmsPage.Create(
            siteId,
            layoutId,
            command.InternalName,
            command.PageTitle,
            command.BrowserTitle,
            command.ParentPageId is { } pid ? CmsPageId.From(pid) : null,
            command.Description,
            command.IconCssClass,
            command.Order,
            command.DisplayInNavWhen,
            command.RequiresEncryption,
            command.OutputCacheDurationSeconds,
            command.IsPublished
        );
        if (pageRes.IsFailure)
        {
            return Result.Failure<CmsPageResponse>(pageRes.PrimaryError);
        }

        var page = pageRes.Value;
        var childErr = ApplyInitialRoutesAndContexts(page, command);
        if (childErr is not null)
        {
            return Result.Failure<CmsPageResponse>(childErr);
        }

        _ = db.Pages.Add(page);
        if (command.SetAsSiteDefault || site.DefaultPageId is null)
        {
            site.SetDefaultPage(page.Id);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(page));
    }

    private static DomainError? ApplyInitialRoutesAndContexts(
        CmsPage page,
        CreateCmsPageCommand command
    )
    {
        if (command.Routes is not null)
        {
            foreach (var route in command.Routes)
            {
                var rRes = page.AddRoute(route);
                if (rRes.IsFailure)
                {
                    return rRes.PrimaryError;
                }
            }
        }

        if (command.Contexts is not null)
        {
            foreach (var (entityKey, idParam) in command.Contexts)
            {
                var cRes = page.AddContext(entityKey, idParam);
                if (cRes.IsFailure)
                {
                    return cRes.PrimaryError;
                }
            }
        }

        return null;
    }
}
