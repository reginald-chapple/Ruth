using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Query returning the prioritized Care-Team Moderation &amp; Guided Review Queue.</summary>
/// <param name="SupportCategoryId">Optional SupportCategory filter.</param>
/// <param name="BranchId">Optional Branch filter.</param>
/// <param name="ModerationState">Optional state filter.</param>
/// <param name="IncludeArchived">Whether to include archived requests.</param>
public sealed record GetCareTeamReviewQueueQuery(
    Guid? SupportCategoryId = null,
    Guid? BranchId = null,
    SupportModerationState? ModerationState = null,
    bool IncludeArchived = false
) : IQuery<IReadOnlyList<SupportRequestResponse>>;
