using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Query to list assessments for a person.</summary>
public sealed record GetPersonAssessmentsQuery(Guid PersonId)
    : IQuery<IReadOnlyList<AssessmentResponse>>;
