using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class CreateMobileApplicationCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateMobileApplicationCommand, MobileApplicationResponse>
{
    public async Task<Result<MobileApplicationResponse>> HandleAsync(
        CreateMobileApplicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = MobileApplication.Create(
            command.Name,
            command.Slug,
            command.BundleIdentifier,
            command.SiteId is { } sid ? CmsSiteId.From(sid) : null,
            command.ApiVersion,
            command.MinimumClientVersion,
            command.LatestClientVersion,
            command.NavigationType,
            command.PrimaryColorHex,
            command.SecondaryColorHex,
            command.AdditionalSettings
        );
        if (res.IsFailure)
        {
            return Result.Failure<MobileApplicationResponse>(res.PrimaryError);
        }

        var exists = await db
            .MobileApplications.AsNoTracking()
            .AnyAsync(a => a.Slug == res.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<MobileApplicationResponse>(
                CmsErrors.Conflict(
                    "MobileApplication.DuplicateSlug",
                    $"MobileApplication slug '{res.Value.Slug}' already exists."
                )
            );
        }

        _ = db.MobileApplications.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(res.Value));
    }
}
