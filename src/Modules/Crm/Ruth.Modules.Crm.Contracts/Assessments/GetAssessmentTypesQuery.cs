using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Query to list configured assessment types.</summary>
public sealed record GetAssessmentTypesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<AssessmentTypeResponse>>;
