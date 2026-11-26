using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class ModerateSupportRequestCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<ModerateSupportRequestCommand, SupportRequestResponse>
{
    public async Task<Result<SupportRequestResponse>> HandleAsync(
        ModerateSupportRequestCommand command,
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
        if (category is null)
        {
            return EngagementErrors.SupportCategoryNotFound;
        }

        var res = request.Moderate(
            category,
            command.Decision,
            command.ModeratorPersonId,
            command.EditedRequestText,
            command.OverrideVisibility,
            command.AssignedCoordinatorPersonId,
            clock.UtcNow
        );
        if (res.IsFailure)
        {
            return Result.Failure<SupportRequestResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SupportProjectionMapper.ToResponse(request, category.Name, redactPublicPii: false);
    }
}
