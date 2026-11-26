namespace Ruth.Modules.Lms.Domain;

/// <summary>Deterministic well-known system GUIDs owned by the LMS module.</summary>
public static class LmsSystemGuids
{
    public static readonly Guid LearningProgram = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    public static readonly Guid LearningCourse = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x02
    );

    public static readonly Guid LearningTerm = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x03
    );

    public static readonly Guid LearningGradingSystem = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x04
    );

    public static readonly Guid LearningClass = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05
    );

    public static readonly Guid LearningActivity = new(
        0x70000003,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x06
    );
}
