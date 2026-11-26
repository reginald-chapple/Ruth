using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Lms.Infrastructure.Configurations;

internal static class LmsModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new LearningGradingSystemConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningGradingScaleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningProgramConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningTermConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningCourseConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningCourseRequirementConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningClassConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningClassContentPageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningClassAnnouncementConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningActivityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningParticipantConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LearningActivityCompletionConfiguration());
    }
}
