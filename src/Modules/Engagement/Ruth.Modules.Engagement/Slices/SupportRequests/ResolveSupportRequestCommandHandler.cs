using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class ResolveSupportRequestCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<ResolveSupportRequestCommand, SupportRequestResponse>
{
    public async Task<Result<SupportRequestResponse>> HandleAsync(
        ResolveSupportRequestCommand command,
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

        request.Resolve(command.OutcomeSummary, command.ArchiveImmediately, clock.UtcNow);
        var catName = await dbContext
            .SupportCategories.AsNoTracking()
            .Where(c => c.Id == SupportCategoryId.From(request.SupportCategoryId))
            .Select(static c => c.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Support";

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SupportProjectionMapper.ToResponse(request, catName, redactPublicPii: false);
    }
}
