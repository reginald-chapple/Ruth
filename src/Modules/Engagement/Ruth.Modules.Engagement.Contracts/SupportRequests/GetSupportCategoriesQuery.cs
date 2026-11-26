using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Query to list SupportCategories.</summary>
/// <param name="ActiveOnly">Whether to return only active categories.</param>
public sealed record GetSupportCategoriesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<SupportCategoryResponse>>;
