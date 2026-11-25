using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Singleton organization profile recording initial setup and permanent self-registration lock.</summary>
public sealed class OrganizationProfile
    : AggregateRoot<OrganizationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasIntegrationEvents
{
    private readonly List<IIntegrationEvent> _integrationEvents = [];

    private OrganizationProfile() { }

    private OrganizationProfile(
        OrganizationId id,
        string name,
        string timeZoneId,
        string primaryBranchName,
        string? primaryBranchShortCode,
        DateTimeOffset completedOnUtc
    )
        : base(id)
    {
        SingletonKey = 1;
        Name = name;
        TimeZoneId = timeZoneId;
        PrimaryBranchName = primaryBranchName;
        PrimaryBranchShortCode = primaryBranchShortCode;
        IsOnboardingCompleted = true;
        SelfRegistrationLocked = true;
        CompletedOnUtc = completedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the constant singleton key enforced by the database unique check constraint.</summary>
    public int SingletonKey { get; private set; } = 1;

    /// <summary>Gets the configured organization name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the configured organization time zone identifier.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    /// <summary>Gets the primary branch or site name.</summary>
    public string PrimaryBranchName { get; private set; } = string.Empty;

    /// <summary>Gets the optional primary branch or site short code.</summary>
    public string? PrimaryBranchShortCode { get; private set; }

    /// <summary>Gets a value indicating whether initial onboarding has completed.</summary>
    public bool IsOnboardingCompleted { get; private set; }

    /// <summary>Gets a value indicating whether anonymous self-registration is permanently locked.</summary>
    public bool SelfRegistrationLocked { get; private set; }

    /// <summary>Gets the UTC timestamp when initial onboarding completed.</summary>
    public DateTimeOffset CompletedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents =>
        _integrationEvents.AsReadOnly();

    /// <summary>Creates a completed, permanently locked organization profile and stages the onboarding integration event.</summary>
    public static Result<OrganizationProfile> CompleteInitialOnboarding(
        string name,
        string timeZoneId,
        string primaryBranchName,
        string? primaryBranchShortCode,
        DateTimeOffset completedOnUtc
    )
    {
        var nameResult = OnboardingPolicy.ValidateRequiredText(
            name,
            OnboardingPolicy.OrganizationNameMaxLength,
            IdentityErrors.OrganizationNameRequired,
            IdentityErrors.OrganizationNameTooLong
        );
        var timeZoneResult = OnboardingPolicy.ValidateTimeZoneId(timeZoneId);
        var branchResult = OnboardingPolicy.ValidateRequiredText(
            primaryBranchName,
            OnboardingPolicy.BranchNameMaxLength,
            IdentityErrors.PrimaryBranchNameRequired,
            IdentityErrors.PrimaryBranchNameTooLong
        );
        var shortCodeResult = OnboardingPolicy.ValidateOptionalText(
            primaryBranchShortCode,
            OnboardingPolicy.BranchShortCodeMaxLength,
            IdentityErrors.PrimaryBranchShortCodeTooLong
        );

        var combined = Result.Combine(nameResult, timeZoneResult, branchResult, shortCodeResult);
        if (combined.IsFailure)
        {
            return Result.Failure<OrganizationProfile>(combined.Errors);
        }

        var utcCompleted = completedOnUtc.ToUniversalTime();
        var id = OrganizationId.Create();
        var profile = new OrganizationProfile(
            id,
            nameResult.Value,
            timeZoneResult.Value,
            branchResult.Value,
            shortCodeResult.Value,
            utcCompleted
        );

        profile._integrationEvents.Add(
            new OrganizationOnboardedIntegrationEvent(
                id.Value,
                profile.Name,
                profile.TimeZoneId,
                utcCompleted
            )
        );

        return profile;
    }

    /// <summary>Rejects repeat onboarding attempts once self-registration is locked.</summary>
    public Result EnsureCanCompleteOnboarding()
    {
        return IsOnboardingCompleted || SelfRegistrationLocked
            ? IdentityErrors.OnboardingAlreadyCompleted
            : Result.Success();
    }

    /// <inheritdoc />
    public void ClearIntegrationEvents()
    {
        _integrationEvents.Clear();
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
