using System.Text.RegularExpressions;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Concrete event registration instance linking capacity, registration windows, and optional overrides to a RegistrationTemplate.</summary>
public sealed partial class RegistrationInstance
    : AggregateRoot<RegistrationInstanceId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Events.RegistrationInstance";

    private RegistrationInstance() { }

    private RegistrationInstance(RegistrationInstanceId id)
        : base(id) { }

    public RegistrationTemplateId TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int? MaxAttendees { get; private set; }
    public DateTimeOffset? RegistrationOpensUtc { get; private set; }
    public DateTimeOffset? RegistrationClosesUtc { get; private set; }
    public DateOnly? PaymentDeadlineDate { get; private set; }
    public decimal? CostOverridePerRegistrant { get; private set; }
    public decimal? DepositOverridePerRegistrant { get; private set; }
    public Guid? EventItemOccurrenceId { get; private set; }
    public Guid? LinkedTargetGroupId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? AdditionalInstructions { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<RegistrationInstance> Create(RegistrationInstanceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var instance = new RegistrationInstance(RegistrationInstanceId.Create());
        var updated = instance.Update(spec);
        return updated.IsFailure ? Result.Failure<RegistrationInstance>(updated.Errors) : instance;
    }

    public Result Update(RegistrationInstanceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (!IsValidSpec(spec, out var slug))
        {
            return EventsRegistrationErrors.InstanceInvalid;
        }

        TemplateId = RegistrationTemplateId.From(spec.TemplateId);
        Name = spec.Name.Trim();
        Slug = slug;
        MaxAttendees = spec.MaxAttendees;
        RegistrationOpensUtc = spec.RegistrationOpensUtc?.ToUniversalTime();
        RegistrationClosesUtc = spec.RegistrationClosesUtc?.ToUniversalTime();
        PaymentDeadlineDate = spec.PaymentDeadlineDate;
        CostOverridePerRegistrant = RoundOptional(spec.CostOverridePerRegistrant);
        DepositOverridePerRegistrant = RoundOptional(spec.DepositOverridePerRegistrant);
        EventItemOccurrenceId = NormalizeGuid(spec.EventItemOccurrenceId);
        LinkedTargetGroupId = NormalizeGuid(spec.LinkedTargetGroupId);
        BranchId = NormalizeGuid(spec.BranchId);
        ContactEmail = TrimOptional(spec.ContactEmail, 254);
        AdditionalInstructions = TrimOptional(spec.AdditionalInstructions, 2000);
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static bool IsValidSpec(RegistrationInstanceSpec spec, out string slug)
    {
        slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        return spec.TemplateId != Guid.Empty
            && !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && IsValidWindowAndOverrides(spec);
    }

    private static bool IsValidWindowAndOverrides(RegistrationInstanceSpec spec)
    {
        var validWindow =
            !spec.RegistrationOpensUtc.HasValue
            || !spec.RegistrationClosesUtc.HasValue
            || spec.RegistrationClosesUtc.Value >= spec.RegistrationOpensUtc.Value;
        var validCap = !spec.MaxAttendees.HasValue || spec.MaxAttendees.Value > 0;
        var validCost =
            !spec.CostOverridePerRegistrant.HasValue || spec.CostOverridePerRegistrant.Value >= 0m;
        var validDep =
            !spec.DepositOverridePerRegistrant.HasValue
            || spec.DepositOverridePerRegistrant.Value >= 0m;
        return validWindow && validCap && validCost && validDep;
    }

    private static decimal? RoundOptional(decimal? value)
    {
        return value.HasValue ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) : null;
    }

    public bool IsOpenAt(DateTimeOffset nowUtc)
    {
        return IsActive
            && (!RegistrationOpensUtc.HasValue || nowUtc >= RegistrationOpensUtc.Value)
            && (!RegistrationClosesUtc.HasValue || nowUtc <= RegistrationClosesUtc.Value);
    }

    public decimal EffectiveBaseCost(RegistrationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return CostOverridePerRegistrant ?? template.BaseCostPerRegistrant;
    }

    public decimal EffectiveMinimumDeposit(RegistrationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var baseCost = EffectiveBaseCost(template);
        var rawDeposit = DepositOverridePerRegistrant
            ?? template.MinimumInitialDepositPerRegistrant;
        return Math.Min(baseCost, rawDeposit);
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }
}
