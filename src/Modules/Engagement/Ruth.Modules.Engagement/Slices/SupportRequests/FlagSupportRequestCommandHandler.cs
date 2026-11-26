using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class FlagSupportRequestCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<FlagSupportRequestCommand, SupportRequestResponse>
{
    public async Task<Result<SupportRequestResponse>> HandleAsync(
        FlagSupportRequestCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var request = await dbContext
            .SupportRequests.Include(static r => r.Comments)
            .FirstOrDefaultAsync(
                r => r.Id == SupportRequestId.From(command.SupportRequestId),
                cancellationToken
            );
        if (request is null)
        {
            return EngagementErrors.SupportRequestNotFound;
        }

        var category = await dbContext
            .SupportCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == SupportCategoryId.From(request.SupportCategoryId),
                cancellationToken
            );
        request.FlagForReview(category?.AutoFlagHoldThreshold ?? 3);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SupportProjectionMapper.ToResponse(
            request,
            category?.Name ?? "Support",
            redactPublicPii: true
        );
    }
}
