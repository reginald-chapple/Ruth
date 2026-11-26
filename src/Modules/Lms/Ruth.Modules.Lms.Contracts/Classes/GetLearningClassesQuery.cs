using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Lms.Contracts.Classes;

/// <summary>Lists LearningClasses optionally filtered by course.</summary>
/// <param name="LearningCourseId">Optional course identifier.</param>
public sealed record GetLearningClassesQuery(Guid? LearningCourseId = null)
    : IQuery<IReadOnlyList<LearningClassResponse>>;
