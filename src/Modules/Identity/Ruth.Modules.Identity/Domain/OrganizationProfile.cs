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
        string primaryCampusName,
        string? primaryCampusShortCode,
        DateTimeOffset completedOnUtc
    )
        : base(id)
    {
        SingletonKey = 1;
        Name = name;
        TimeZoneId = timeZoneId;
        PrimaryCampusName = primaryCampusName;
        PrimaryCampusShortCode = primaryCampusShortCode;
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

    /// <summary>Gets the primary campus name.</summary>
    public string PrimaryCampusName { get; private set; } = string.Empty;

    /// <summary>Gets the optional primary campus short code.</summary>
    public string? PrimaryCampusShortCode { get; private set; }

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
        string primaryCampusName,
        string? primaryCampusShortCode,
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
        var campusResult = OnboardingPolicy.ValidateRequiredText(
            primaryCampusName,
            OnboardingPolicy.CampusNameMaxLength,
            IdentityErrors.PrimaryCampusNameRequired,
            IdentityErrors.PrimaryCampusNameTooLong
        );
        var shortCodeResult = OnboardingPolicy.ValidateOptionalText(
            primaryCampusShortCode,
            OnboardingPolicy.CampusShortCodeMaxLength,
            IdentityErrors.PrimaryCampusShortCodeTooLong
        );

        var combined = Result.Combine(nameResult, timeZoneResult, campusResult, shortCodeResult);
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
            campusResult.Value,
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
