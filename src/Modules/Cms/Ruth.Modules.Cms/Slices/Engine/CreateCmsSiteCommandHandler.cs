using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateCmsSiteCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateCmsSiteCommand, CmsSiteResponse>
{
    public async Task<Result<CmsSiteResponse>> HandleAsync(
        CreateCmsSiteCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var siteRes = CmsSite.Create(
            command.Name,
            command.Slug,
            command.Description,
            command.ThemeId is { } tid ? CmsThemeId.From(tid) : null,
            command.BranchId,
            command.SiteType,
            command.EnablePageViews
        );
        if (siteRes.IsFailure)
        {
            return Result.Failure<CmsSiteResponse>(siteRes.PrimaryError);
        }

        var site = siteRes.Value;
        var slugExists = await db
            .Sites.AsNoTracking()
            .AnyAsync(s => s.Slug == site.Slug, cancellationToken);
        if (slugExists)
        {
            return Result.Failure<CmsSiteResponse>(
                CmsErrors.Conflict("Site.DuplicateSlug", $"Site slug '{site.Slug}' already exists.")
            );
        }

        if (command.Domains is not null)
        {
            foreach (var rawDomain in command.Domains)
            {
                var dRes = site.AddDomain(rawDomain);
                if (dRes.IsFailure)
                {
                    return Result.Failure<CmsSiteResponse>(dRes.PrimaryError);
                }

                var domainTaken = await db
                    .SiteDomains.AsNoTracking()
                    .AnyAsync(d => d.Domain == dRes.Value.Domain, cancellationToken);
                if (domainTaken)
                {
                    return Result.Failure<CmsSiteResponse>(
                        CmsErrors.Conflict(
                            "SiteDomain.Taken",
                            $"Domain '{dRes.Value.Domain}' is already assigned to another site."
                        )
                    );
                }
            }
        }

        _ = db.Sites.Add(site);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(site));
    }
}
