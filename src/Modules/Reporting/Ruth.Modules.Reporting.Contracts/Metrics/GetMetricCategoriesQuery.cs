using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Metrics;

/// <summary>Lists MetricCategories.</summary>
public sealed record GetMetricCategoriesQuery : IQuery<IReadOnlyList<MetricCategoryResponse>>;
