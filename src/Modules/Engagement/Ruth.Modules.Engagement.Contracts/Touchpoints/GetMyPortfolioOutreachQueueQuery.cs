using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Query returning the Mobile/Web My Portfolio summary and prioritized outreach queue.</summary>
/// <param name="OwnerPersonId">Optional owner Person filter.</param>
/// <param name="BranchId">Optional Branch filter.</param>
public sealed record GetMyPortfolioOutreachQueueQuery(
    Guid? OwnerPersonId = null,
    Guid? BranchId = null
) : IQuery<PortfolioOutreachQueueResponse>;
