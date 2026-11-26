using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain.SupportRequests;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal static class SupportProjectionMapper
{
    public static SupportCategoryResponse ToResponse(SupportCategory entity)
    {
        return new SupportCategoryResponse(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.RequiresModeration,
            entity.DefaultExpirationDays,
            entity.AllowAnonymousIntake,
            entity.AllowPublicCommunityWall,
            entity.AllowComments,
            entity.AutoFlagHoldThreshold,
            entity.DefaultCoordinatorPersonId,
            entity.IsActive
        );
    }

    public static SupportRequestResponse ToResponse(
        SupportRequest entity,
        string categoryName,
        bool redactPublicPii
    )
    {
        var visibleComments = entity
            .Comments.Where(c => !redactPublicPii || !c.IsInternalCareNote)
            .OrderBy(static c => c.CreatedAtUtc)
            .Select(c => new SupportRequestCommentResponse(
                c.Id.Value,
                redactPublicPii ? null : c.AuthorPersonId,
                c.AuthorDisplayName,
                c.Body,
                c.IsInternalCareNote,
                c.CreatedAtUtc
            ))
            .ToArray();

        return new SupportRequestResponse(
            entity.Id.Value,
            entity.SupportCategoryId,
            categoryName,
            redactPublicPii ? null : entity.RequesterPersonId,
            entity.FirstName,
            redactPublicPii ? null : entity.LastName,
            redactPublicPii ? null : entity.Email,
            entity.BranchId,
            redactPublicPii ? null : entity.AssignedCareCoordinatorPersonId,
            entity.RequestText,
            entity.AnswerOrOutcomeSummary,
            entity.VisibilityLevel,
            entity.ModerationState,
            entity.IsUrgent,
            entity.AllowComments,
            entity.CommunitySupportCount,
            entity.FlagCount,
            entity.EnteredAtUtc,
            entity.ExpiresAtUtc,
            entity.ResolvedAtUtc,
            visibleComments
        );
    }
}
