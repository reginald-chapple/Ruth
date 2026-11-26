namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Hierarchical MetricCategory response.</summary>
/// <param name="Id">Category identifier.</param>
/// <param name="Name">Category name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="ParentCategoryId">Optional parent category identifier.</param>
/// <param name="Order">Display sort order.</param>
public sealed record MetricCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    int Order
);
