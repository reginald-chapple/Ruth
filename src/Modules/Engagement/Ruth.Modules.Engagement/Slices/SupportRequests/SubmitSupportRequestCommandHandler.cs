using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class SubmitSupportRequestCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<SubmitSupportRequestCommand, SupportRequestResponse>
{
    public async Task<Result<SupportRequestResponse>> HandleAsync(
        SubmitSupportRequestCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var category = await dbContext
            .SupportCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == SupportCategoryId.From(command.SupportCategoryId) && c.IsActive,
                cancellationToken
            );
        if (category is null)
        {
            return EngagementErrors.SupportCategoryNotFound;
        }

        var now = clock.UtcNow;
        var submitted = SupportRequest.Submit(
            category,
            new SupportRequestIntakeSpec(
                command.FirstName,
                command.RequestText,
                command.LastName,
                command.Email,
                command.RequesterPersonId,
                command.BranchId,
                command.VisibilityLevel,
                command.IsUrgent,
                command.AllowComments
            ),
            now
        );
        if (submitted.IsFailure)
        {
            return Result.Failure<SupportRequestResponse>(submitted.Errors);
        }

        var req = submitted.Value;
        _ = dbContext.SupportRequests.Add(req);
        StageNotificationsAndEvents(req, category, now);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SupportProjectionMapper.ToResponse(req, category.Name, redactPublicPii: false);
    }

    private void StageNotificationsAndEvents(
        SupportRequest req,
        SupportCategory category,
        DateTimeOffset now
    )
    {
        if (req.AssignedCareCoordinatorPersonId is { } coordinatorId)
        {
            _ = dbContext.NotificationMessages.Add(
                NotificationMessage.Create(
                    coordinatorId,
                    req.IsUrgent
                        ? $"Urgent {category.Name} Request"
                        : $"New {category.Name} Request",
                    $"{req.FirstName} submitted a {category.Name} support request.",
                    NotificationCategory.SupportCare,
                    "/engagement/care",
                    req.IsUrgent ? "#D13438" : "#0F6CBD"
                )
            );
        }

        if (req.RequesterPersonId is { } personId)
        {
            var followEvt = FollowingEventNotification.Create(
                "Ruth.Crm.Person",
                personId,
                "SupportRequestSubmitted",
                $"{req.FirstName} submitted a {category.Name} support request.",
                now
            );
            if (followEvt.IsSuccess)
            {
                _ = dbContext.FollowingEventNotifications.Add(followEvt.Value);
            }
        }

        dbContext.EnqueueIntegrationEvent(
            new SupportRequestSubmittedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                req.Id.Value,
                req.SupportCategoryId,
                req.RequesterPersonId,
                req.BranchId,
                req.IsUrgent
            )
        );
    }
}
