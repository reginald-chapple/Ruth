using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Creates a MetricCategory for organizing operational metrics.</summary>
/// <param name="Name">Category name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="ParentCategoryId">Optional parent category identifier.</param>
/// <param name="Order">Sort order.</param>
public sealed record CreateMetricCategoryCommand(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null,
    int Order = 0
) : ICommand<MetricCategoryResponse>;
