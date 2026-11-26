using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Channels;

internal sealed class AddMobilePageCommandHandler(CmsDbContext db)
    : ICommandHandler<AddMobilePageCommand, MobileApplicationResponse>
{
    public async Task<Result<MobileApplicationResponse>> HandleAsync(
        AddMobilePageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var appId = MobileApplicationId.From(command.MobileApplicationId);
        var app = await db
            .MobileApplications.Include(static a => a.Layouts)
            .Include(static a => a.Pages)
            .Include(static a => a.DeepLinks)
            .FirstOrDefaultAsync(a => a.Id == appId, cancellationToken);
        if (app is null)
        {
            return Result.Failure<MobileApplicationResponse>(
                CmsErrors.NotFound("MobileApplication", command.MobileApplicationId.ToString("D"))
            );
        }

        var res = app.AddPage(
            command.Title,
            command.PageKey,
            command.Route,
            command.MobileLayoutId is { } lid ? MobileLayoutId.From(lid) : null,
            command.IconName,
            command.Order,
            command.ShowInNavigation,
            command.RequiresAuthentication,
            command.ContentChannelId is { } cid ? ContentChannelId.From(cid) : null,
            command.AdaptiveMessageKey,
            command.BlocksJson
        );
        if (res.IsFailure)
        {
            return Result.Failure<MobileApplicationResponse>(res.PrimaryError);
        }

        _ = db.MobilePages.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(app));
    }
}
