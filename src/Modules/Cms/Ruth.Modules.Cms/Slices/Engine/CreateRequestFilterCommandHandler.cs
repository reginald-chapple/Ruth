using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateRequestFilterCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateRequestFilterCommand, RequestFilterResponse>
{
    public async Task<Result<RequestFilterResponse>> HandleAsync(
        CreateRequestFilterCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var filterRes = RequestFilter.Create(
            command.Name,
            command.Key,
            command.SiteId is { } sid ? CmsSiteId.From(sid) : null,
            command.FilterMode,
            command.Rules,
            command.IsActive
        );
        if (filterRes.IsFailure)
        {
            return Result.Failure<RequestFilterResponse>(filterRes.PrimaryError);
        }

        var exists = await db
            .RequestFilters.AsNoTracking()
            .AnyAsync(f => f.Key == filterRes.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<RequestFilterResponse>(
                CmsErrors.Conflict(
                    "RequestFilter.DuplicateKey",
                    $"RequestFilter key '{filterRes.Value.Key}' already exists."
                )
            );
        }

        _ = db.RequestFilters.Add(filterRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(filterRes.Value));
    }
}
