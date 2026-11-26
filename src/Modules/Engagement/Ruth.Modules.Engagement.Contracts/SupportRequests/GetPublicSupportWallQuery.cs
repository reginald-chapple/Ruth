using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Query returning approved, non-expired PublicCommunityWall SupportRequests with PII redacted.</summary>
/// <param name="SupportCategoryId">Optional SupportCategory filter.</param>
/// <param name="BranchId">Optional Branch filter.</param>
public sealed record GetPublicSupportWallQuery(
    Guid? SupportCategoryId = null,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<SupportRequestResponse>>;
