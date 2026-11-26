using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.Modules.Lms.Domain.Programs;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Lms;

internal static class LmsEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            LmsSystemGuids.LearningProgram,
            LearningProgram.EntityTypeKeyName,
            nameof(LearningProgram),
            "Learning Program",
            "Lms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            LmsSystemGuids.LearningCourse,
            LearningCourse.EntityTypeKeyName,
            nameof(LearningCourse),
            "Learning Course",
            "Lms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            LmsSystemGuids.LearningTerm,
            LearningTerm.EntityTypeKeyName,
            nameof(LearningTerm),
            "Learning Term",
            "Lms",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            LmsSystemGuids.LearningGradingSystem,
            LearningGradingSystem.EntityTypeKeyName,
            nameof(LearningGradingSystem),
            "Learning Grading System",
            "Lms",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            LmsSystemGuids.LearningClass,
            LearningClass.EntityTypeKeyName,
            nameof(LearningClass),
            "Learning Class",
            "Lms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            LmsSystemGuids.LearningActivity,
            LearningActivity.EntityTypeKeyName,
            nameof(LearningActivity),
            "Learning Activity",
            "Lms",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
