using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Domain.Classes;
using Ruth.Modules.Lms.Domain.Courses;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.Modules.Lms.Domain.Programs;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Lms.Infrastructure;

/// <summary>EF Core database context exclusively owning the lms PostgreSQL schema.</summary>
public sealed class LmsDbContext(DbContextOptions<LmsDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "lms";

    protected override string Schema => SchemaName;

    public DbSet<LearningGradingSystem> GradingSystems => Set<LearningGradingSystem>();
    public DbSet<LearningGradingScale> GradingScales => Set<LearningGradingScale>();
    public DbSet<LearningProgram> Programs => Set<LearningProgram>();
    public DbSet<LearningTerm> Terms => Set<LearningTerm>();
    public DbSet<LearningCourse> Courses => Set<LearningCourse>();
    public DbSet<LearningCourseRequirement> CourseRequirements => Set<LearningCourseRequirement>();
    public DbSet<LearningClass> Classes => Set<LearningClass>();
    public DbSet<LearningClassContentPage> ContentPages => Set<LearningClassContentPage>();
    public DbSet<LearningClassAnnouncement> Announcements => Set<LearningClassAnnouncement>();
    public DbSet<LearningActivity> Activities => Set<LearningActivity>();
    public DbSet<LearningParticipant> Participants => Set<LearningParticipant>();
    public DbSet<LearningActivityCompletion> ActivityCompletions =>
        Set<LearningActivityCompletion>();

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.LmsModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
