using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Lms.Domain.Programs;

/// <summary>LearningProgram aggregate supporting On-Demand self-paced and Term/Cohort schedules.</summary>
public sealed class LearningProgram
    : AggregateRoot<LearningProgramId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Lms.LearningProgram";

    private LearningProgram() { }

    private LearningProgram(LearningProgramId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string PublicName { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public LearningProgramConfigurationMode ConfigurationMode { get; private set; }
    public Guid? CategoryId { get; private set; }
    public LearningGradingSystemId? DefaultGradingSystemId { get; private set; }
    public string? HighlightColor { get; private set; }
    public string? IconCssClass { get; private set; }
    public bool IsPublic { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public bool IsCompletionStatusTracked { get; private set; } = true;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<LearningProgram> Create(
        string name,
        string? publicName,
        LearningProgramConfigurationMode configurationMode,
        string? summary = null,
        string? description = null,
        Guid? categoryId = null,
        LearningGradingSystemId? defaultGradingSystemId = null,
        string? highlightColor = null,
        string? iconCssClass = null,
        bool isPublic = true,
        bool isCompletionStatusTracked = true
    )
    {
        return string.IsNullOrWhiteSpace(name)
            ? Result.Failure<LearningProgram>(
                LmsErrors.Validation("Program.NameRequired", "LearningProgram name is required.")
            )
            : Result.Success(
                new LearningProgram(LearningProgramId.Create())
                {
                    Name = name.Trim(),
                    PublicName = string.IsNullOrWhiteSpace(publicName)
                        ? name.Trim()
                        : publicName.Trim(),
                    ConfigurationMode = configurationMode,
                    Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    CategoryId = categoryId,
                    DefaultGradingSystemId = defaultGradingSystemId,
                    HighlightColor = string.IsNullOrWhiteSpace(highlightColor)
                        ? null
                        : highlightColor.Trim(),
                    IconCssClass = string.IsNullOrWhiteSpace(iconCssClass)
                        ? null
                        : iconCssClass.Trim(),
                    IsPublic = isPublic,
                    IsActive = true,
                    IsCompletionStatusTracked = isCompletionStatusTracked,
                }
            );
    }
}
