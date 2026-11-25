using System.Net.Mail;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain;

/// <summary>Organization operating unit (Branch, Site, Chapter, or Center) aggregate owning schedules, topics, timezone, and primary location link.</summary>
public sealed class Branch
    : AggregateRoot<BranchId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for Branch.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Branch";

    private readonly List<BranchSchedule> _schedules = [];
    private readonly List<BranchTopic> _topics = [];

    private Branch() { }

    private Branch(BranchId id, Guid systemGuid, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the unique branch or site name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name used for uniqueness enforcement.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional branch description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional short code (e.g. HQ, WEST).</summary>
    public string? ShortCode { get; private set; }

    /// <summary>Gets whether the branch is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether this branch is system-protected.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the optional website URL.</summary>
    public string? Url { get; private set; }

    /// <summary>Gets the optional phone number.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Gets the optional IANA or system TimeZoneInfo identifier.</summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>Gets the primary physical location identifier.</summary>
    public LocationId? LocationId { get; private set; }

    /// <summary>Gets the optional branch director/leader person identifier.</summary>
    public Guid? LeaderPersonId { get; private set; }

    /// <summary>Gets the optional branch status defined value identifier.</summary>
    public Guid? BranchStatusValueId { get; private set; }

    /// <summary>Gets the optional branch type defined value identifier.</summary>
    public Guid? BranchTypeValueId { get; private set; }

    /// <summary>Gets the optional branch team group identifier.</summary>
    public Guid? TeamGroupId { get; private set; }

    /// <summary>Gets the optional photo binary file identifier.</summary>
    public Guid? PhotoId { get; private set; }

    /// <summary>Gets the date the branch opened.</summary>
    public DateOnly? OpenedDate { get; private set; }

    /// <summary>Gets the date the branch closed.</summary>
    public DateOnly? ClosedDate { get; private set; }

    /// <summary>Gets the optional BLE proximity beacon identifier (0..65535).</summary>
    public int? BeaconId { get; private set; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets the ordered branch schedules.</summary>
    public IReadOnlyCollection<BranchSchedule> Schedules => _schedules.AsReadOnly();

    /// <summary>Gets the branch contact topics.</summary>
    public IReadOnlyCollection<BranchTopic> Topics => _topics.AsReadOnly();

    /// <summary>Gets the condensed branch display name (ShortCode or Name without trailing " Branch" / " Site").</summary>
    public string CondensedName =>
        !string.IsNullOrWhiteSpace(ShortCode) ? ShortCode
        : Name.EndsWith(" Branch", StringComparison.OrdinalIgnoreCase)
            ? Name[..^" Branch".Length].TrimEnd()
        : Name.EndsWith(" Site", StringComparison.OrdinalIgnoreCase)
            ? Name[..^" Site".Length].TrimEnd()
        : Name;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Resolves the branch local wall-clock time from a UTC instant.</summary>
    public DateTimeOffset GetCurrentLocalTime(DateTimeOffset utcNow)
    {
        return string.IsNullOrWhiteSpace(TimeZoneId) ? utcNow
            : TimeZoneInfo.TryFindSystemTimeZoneById(TimeZoneId, out var tz)
                ? TimeZoneInfo.ConvertTime(utcNow, tz)
            : utcNow;
    }

    internal static Result<Branch> Create(
        BranchInput input,
        bool isSystem = false,
        Guid? systemGuid = null
    )
    {
        var id = BranchId.Create();
        var branch = new Branch(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value,
            isSystem
        );
        var apply = branch.Apply(input);
        return apply.IsFailure ? Result.Failure<Branch>(apply.Errors) : Result.Success(branch);
    }

    internal Result Update(BranchInput input)
    {
        return Apply(input);
    }

    internal void RepointLeaderPerson(Guid survivingPersonId)
    {
        LeaderPersonId = survivingPersonId;
    }

    private Result Apply(BranchInput input)
    {
        var validation = ValidateCoreFields(input);
        if (validation.IsFailure)
        {
            return validation;
        }

        var topicsResult = BuildTopics(input.Topics);
        if (topicsResult.IsFailure)
        {
            return Result.Failure(topicsResult.Errors);
        }

        ApplyValidatedScalars(input);
        ReplaceSchedules(input.Schedules);
        if (input.Topics is not null)
        {
            ReplaceTopics(topicsResult.Value);
        }

        return Result.Success();
    }

    private static Result ValidateCoreFields(BranchInput input)
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            input.Name,
            CoreTextPolicy.NameMaxLength,
            CoreLocationErrors.BranchNameRequired,
            CoreLocationErrors.BranchNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            input.Description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var shortResult = CoreTextPolicy.ValidateOptionalText(
            input.ShortCode,
            maxLength: 50,
            allowNewLines: false
        );
        var phoneResult = CoreTextPolicy.ValidateOptionalText(
            input.PhoneNumber,
            maxLength: 50,
            allowNewLines: false
        );
        var combined = Result.Combine(nameResult, descResult, shortResult, phoneResult);
        return combined.IsFailure ? combined : ValidateDomainConstraints(input);
    }

    private static Result ValidateDomainConstraints(BranchInput input)
    {
        var endpointCheck = ValidateUrlAndTimeZone(input);
        return endpointCheck.IsFailure ? endpointCheck : ValidateDatesAndBeacon(input);
    }

    private static Result ValidateUrlAndTimeZone(BranchInput input)
    {
        if (!IsValidHttpUrl(input.Url))
        {
            return CoreLocationErrors.BranchUrlInvalid;
        }

        var invalidTz =
            !string.IsNullOrWhiteSpace(input.TimeZoneId)
            && !TimeZoneInfo.TryFindSystemTimeZoneById(input.TimeZoneId.Trim(), out _);
        return invalidTz ? CoreLocationErrors.BranchTimeZoneInvalid : Result.Success();
    }

    private static Result ValidateDatesAndBeacon(BranchInput input)
    {
        var invalidDates =
            input.OpenedDate is { } opened && input.ClosedDate is { } closed && closed < opened;
        return invalidDates ? CoreLocationErrors.BranchDateRangeInvalid
            : input.BeaconId is < 0 or > ushort.MaxValue ? CoreLocationErrors.BranchBeaconInvalid
            : Result.Success();
    }

    private void ApplyValidatedScalars(BranchInput input)
    {
        Name = input.Name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        Description = string.IsNullOrWhiteSpace(input.Description)
            ? null
            : input.Description.Trim();
        ShortCode = string.IsNullOrWhiteSpace(input.ShortCode) ? null : input.ShortCode.Trim();
        IsActive = input.IsActive;
        Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber)
            ? null
            : input.PhoneNumber.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(input.TimeZoneId) ? null : input.TimeZoneId.Trim();
        LocationId = input.LocationId;
        LeaderPersonId = input.LeaderPersonId;
        BranchStatusValueId = input.BranchStatusValueId;
        BranchTypeValueId = input.BranchTypeValueId;
        PhotoId = input.PhotoId;
        OpenedDate = input.OpenedDate;
        ClosedDate = input.ClosedDate;
        BeaconId = input.BeaconId;
        Order = Math.Max(0, input.Order);
    }

    private void ReplaceSchedules(IReadOnlyList<BranchScheduleDto>? schedules)
    {
        if (schedules is null)
        {
            return;
        }

        _schedules.Clear();
        var orderIndex = 0;
        foreach (var item in schedules.OrderBy(static s => s.Order))
        {
            if (item.ScheduleId == Guid.Empty)
            {
                continue;
            }

            _schedules.Add(
                new BranchSchedule(
                    Guid.CreateVersion7(),
                    Id,
                    item.ScheduleId,
                    item.ScheduleTypeValueId,
                    orderIndex++
                )
            );
        }
    }

    private Result<List<BranchTopic>> BuildTopics(IReadOnlyList<BranchTopicDto>? topics)
    {
        if (topics is null)
        {
            return Result.Success(new List<BranchTopic>());
        }

        var built = new List<BranchTopic>(topics.Count);
        foreach (var item in topics)
        {
            if (item.TopicTypeValueId == Guid.Empty || !IsValidEmail(item.Email))
            {
                return CoreLocationErrors.BranchTopicEmailInvalid;
            }

            built.Add(
                new BranchTopic(
                    Guid.CreateVersion7(),
                    Id,
                    item.TopicTypeValueId,
                    item.Email.Trim(),
                    item.IsPublic
                )
            );
        }

        return Result.Success(built);
    }

    private void ReplaceTopics(List<BranchTopic> topics)
    {
        if (topics.Count == 0 && _topics.Count == 0)
        {
            return;
        }

        _topics.Clear();
        _topics.AddRange(topics);
    }

    private static bool IsValidHttpUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url)
            || (
                Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                && (
                    string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        uri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );
    }

    private static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && email.Trim().Length <= 254
            && MailAddress.TryCreate(email.Trim(), out var parsed)
            && string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
