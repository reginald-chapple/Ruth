using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Portal;

/// <summary>Queries the Public Course Catalog and optional Learner Portal enrollment state.</summary>
/// <param name="PersonId">Optional authenticated learner PersonId for prerequisite and enrollment status.</param>
/// <param name="ProgramId">Optional program filter.</param>
public sealed record GetPublicCourseCatalogQuery(Guid? PersonId = null, Guid? ProgramId = null)
    : IQuery<IReadOnlyList<PublicCourseCatalogItemResponse>>;
