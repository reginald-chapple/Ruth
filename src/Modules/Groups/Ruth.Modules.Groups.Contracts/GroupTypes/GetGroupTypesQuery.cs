using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Lists all configured GroupTypes.</summary>
public sealed record GetGroupTypesQuery(bool NavigationOnly = false)
    : IQuery<IReadOnlyList<GroupTypeSummaryResponse>>;
