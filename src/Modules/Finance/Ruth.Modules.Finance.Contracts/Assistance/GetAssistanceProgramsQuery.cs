using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Query to list assistance and grant programs.</summary>
public sealed record GetAssistanceProgramsQuery(
    AssistanceProgramCategory? Category = null,
    Guid? BranchId = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<AssistanceProgramResponse>>;
