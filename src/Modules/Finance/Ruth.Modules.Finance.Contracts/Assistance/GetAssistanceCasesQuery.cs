using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Query to list assistance cases filtered optionally by program, applicant, status, or branch.</summary>
public sealed record GetAssistanceCasesQuery(
    Guid? ProgramId = null,
    Guid? ApplicantPersonId = null,
    AssistanceCaseStatus? Status = null,
    Guid? BranchId = null,
    int Limit = 100
) : IQuery<IReadOnlyList<AssistanceCaseResponse>>;
