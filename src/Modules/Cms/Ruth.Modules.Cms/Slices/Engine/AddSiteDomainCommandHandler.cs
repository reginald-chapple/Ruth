using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class AddSiteDomainCommandHandler(CmsDbContext db)
    : ICommandHandler<AddSiteDomainCommand, CmsSiteResponse>
{
    public async Task<Result<CmsSiteResponse>> HandleAsync(
        AddSiteDomainCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var siteId = CmsSiteId.From(command.SiteId);
        var site = await db
            .Sites.Include(static s => s.Domains)
            .FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
        if (site is null)
        {
            return Result.Failure<CmsSiteResponse>(
                CmsErrors.NotFound("Site", command.SiteId.ToString("D"))
            );
        }

        var addRes = site.AddDomain(command.Domain, command.IsPrimary);
        if (addRes.IsFailure)
        {
            return Result.Failure<CmsSiteResponse>(addRes.PrimaryError);
        }

        var domainTaken = await db
            .SiteDomains.AsNoTracking()
            .AnyAsync(d => d.Domain == addRes.Value.Domain, cancellationToken);
        if (domainTaken)
        {
            return Result.Failure<CmsSiteResponse>(
                CmsErrors.Conflict(
                    "SiteDomain.Taken",
                    $"Domain '{addRes.Value.Domain}' is already assigned."
                )
            );
        }

        _ = db.SiteDomains.Add(addRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(site));
    }
}
