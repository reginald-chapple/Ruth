using System.Text.RegularExpressions;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Campaigns;

/// <summary>Peer-to-peer, team, or project fundraising campaign aggregate pairing a Group with a target FinancialAccount.</summary>
public sealed partial class FundraisingCampaign
    : AggregateRoot<FundraisingCampaignId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    /// <summary>Canonical entity type key for ACL evaluation and entity registry.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.FundraisingCampaign";

    private readonly List<CampaignParticipant> _participants = [];
    private readonly List<CampaignContributionCredit> _credits = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private FundraisingCampaign() { }

    private FundraisingCampaign(FundraisingCampaignId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid GroupId { get; private set; }
    public FinancialAccountId FinancialAccountId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal CampaignGoalAmount { get; private set; }
    public decimal? TeamGoalOverrideAmount { get; private set; }
    public decimal EffectiveTeamGoalAmount => TeamGoalOverrideAmount ?? CampaignGoalAmount;
    public decimal? DefaultParticipantGoalAmount { get; private set; }
    public string? Summary { get; private set; }
    public string? PublicDescription { get; private set; }
    public bool AllowTeamGoalOverride { get; private set; } = true;
    public bool AllowParticipantGoalOverride { get; private set; } = true;
    public bool AllowPublicContributions { get; private set; } = true;
    public FinancialGatewayId? DefaultFinancialGatewayId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public FundraisingCampaignStatus Status { get; private set; } =
        FundraisingCampaignStatus.Active;

    public IReadOnlyCollection<CampaignParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<CampaignContributionCredit> Credits => _credits.AsReadOnly();

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    /// <summary>Creates a validated <see cref="FundraisingCampaign" /> aggregate.</summary>
    public static Result<FundraisingCampaign> Create(FundraisingCampaignSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var campaign = new FundraisingCampaign(FundraisingCampaignId.Create());
        var updated = campaign.Update(spec);
        return updated.IsFailure ? Result.Failure<FundraisingCampaign>(updated.Errors) : campaign;
    }

    /// <summary>Validates and updates mutable campaign fields.</summary>
    public Result Update(FundraisingCampaignSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateSpec(spec, out var currency, out var scale);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = spec.Name.Trim();
        Slug = spec.Slug.Trim().ToLowerInvariant();
        GroupId = spec.GroupId;
        FinancialAccountId = FinancialAccountId.From(spec.FinancialAccountId);
        CurrencyCode = currency;
        CampaignGoalAmount = decimal.Round(
            spec.CampaignGoalAmount,
            scale,
            MidpointRounding.AwayFromZero
        );
        DefaultParticipantGoalAmount = spec.DefaultParticipantGoalAmount.HasValue
            ? decimal.Round(
                spec.DefaultParticipantGoalAmount.Value,
                scale,
                MidpointRounding.AwayFromZero
            )
            : null;
        TeamGoalOverrideAmount = spec.TeamGoalOverrideAmount.HasValue
            ? decimal.Round(spec.TeamGoalOverrideAmount.Value, scale, MidpointRounding.AwayFromZero)
            : null;
        Summary = TrimOptional(spec.Summary, 400);
        PublicDescription = TrimOptional(spec.PublicDescription, 4000);
        AllowTeamGoalOverride = spec.AllowTeamGoalOverride;
        AllowParticipantGoalOverride = spec.AllowParticipantGoalOverride;
        AllowPublicContributions = spec.AllowPublicContributions;
        DefaultFinancialGatewayId =
            spec.DefaultFinancialGatewayId is { } gw && gw != Guid.Empty
                ? FinancialGatewayId.From(gw)
                : null;
        BranchId = spec.BranchId is { } br && br != Guid.Empty ? br : null;
        StartDate = spec.StartDate;
        EndDate = spec.EndDate;
        Status = spec.Status;
        ApplyAttributes(spec.Attributes);

        return Result.Success();
    }

    /// <summary>Sets or clears a team goal override when permitted on this campaign.</summary>
    public Result SetTeamGoalOverride(decimal? overrideAmount)
    {
        if (!AllowTeamGoalOverride)
        {
            return FinanceCampaignErrors.TeamGoalOverrideNotAllowed;
        }

        if (!overrideAmount.HasValue)
        {
            TeamGoalOverrideAmount = null;
            return Result.Success();
        }

        var validMoney = Money.CreatePositive(overrideAmount.Value, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return validMoney;
        }

        TeamGoalOverrideAmount = validMoney.Value.Amount;
        return Result.Success();
    }

    /// <summary>Determines whether the campaign accepts public contributions on <paramref name="today" />.</summary>
    public bool IsOpenForPublicContributions(DateOnly today)
    {
        return Status == FundraisingCampaignStatus.Active
            && AllowPublicContributions
            && (StartDate is null || today >= StartDate.Value)
            && (EndDate is null || today <= EndDate.Value);
    }

    /// <summary>Enrolls a participant fundraiser on this campaign.</summary>
    public Result<CampaignParticipant> EnrollParticipant(
        Guid personId,
        string displayName,
        Guid? groupMemberId = null,
        decimal? customGoalAmount = null,
        string? personalMessage = null,
        bool isPublicProfileEnabled = true
    )
    {
        if (personId == Guid.Empty || string.IsNullOrWhiteSpace(displayName))
        {
            return FinanceCampaignErrors.ParticipantInvalid;
        }

        if (_participants.Exists(p => p.PersonId == personId))
        {
            return FinanceCampaignErrors.ParticipantDuplicate;
        }

        _ = IsoCurrencyCatalog.TryGetScale(CurrencyCode, out _, out var scale);
        var participant = new CampaignParticipant(
            CampaignParticipantId.Create(),
            Id,
            personId,
            groupMemberId is { } gm && gm != Guid.Empty ? gm : null,
            displayName.Trim(),
            customGoalAmount: null,
            TrimOptional(personalMessage, 500),
            isFundraisingEnabled: true,
            isPublicProfileEnabled
        );

        var goalUpdate = participant.UpdateGoalAndProfile(
            customGoalAmount,
            AllowParticipantGoalOverride,
            scale,
            personalMessage,
            isFundraisingEnabled: true,
            isPublicProfileEnabled
        );
        if (goalUpdate.IsFailure)
        {
            return Result.Failure<CampaignParticipant>(goalUpdate.Errors);
        }

        _participants.Add(participant);
        return participant;
    }

    /// <summary>Updates a participant's custom fundraising goal override and profile settings.</summary>
    public Result<CampaignParticipant> UpdateParticipantGoal(
        CampaignParticipantId participantId,
        decimal? customGoalAmount,
        string? personalMessage,
        bool isFundraisingEnabled,
        bool isPublicProfileEnabled
    )
    {
        var participant = _participants.Find(p => p.Id == participantId);
        if (participant is null)
        {
            return FinanceCampaignErrors.ParticipantNotFound;
        }

        _ = IsoCurrencyCatalog.TryGetScale(CurrencyCode, out _, out var scale);
        var updated = participant.UpdateGoalAndProfile(
            customGoalAmount,
            AllowParticipantGoalOverride,
            scale,
            personalMessage,
            isFundraisingEnabled,
            isPublicProfileEnabled
        );
        return updated.IsFailure
            ? Result.Failure<CampaignParticipant>(updated.Errors)
            : participant;
    }

    /// <summary>Records a contribution credit tied to a settled financial transaction.</summary>
    public Result<CampaignContributionCredit> RecordContributionCredit(
        FinancialTransactionId transactionId,
        decimal amount,
        string donorDisplayName,
        CampaignDonorPrivacy donorPrivacy,
        DateTimeOffset contributedAtUtc,
        CampaignParticipantId? creditedParticipantId = null,
        Guid? donorPersonId = null,
        string? donorComment = null
    )
    {
        if (string.IsNullOrWhiteSpace(donorDisplayName))
        {
            return FinanceCampaignErrors.ContributionDonorNameRequired;
        }

        var validMoney = Money.CreatePositive(amount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return Result.Failure<CampaignContributionCredit>(validMoney.Errors);
        }

        var participantRes = ResolveCreditedParticipant(creditedParticipantId);
        if (participantRes.IsFailure)
        {
            return Result.Failure<CampaignContributionCredit>(participantRes.Errors);
        }

        var credit = new CampaignContributionCredit(
            CampaignContributionCreditId.Create(),
            Id,
            transactionId,
            participantRes.Value,
            NormalizePersonGuid(donorPersonId),
            donorDisplayName.Trim(),
            validMoney.Value.Amount,
            CurrencyCode,
            donorPrivacy,
            TrimOptional(donorComment, 500),
            contributedAtUtc
        );
        _credits.Add(credit);
        return credit;
    }

    private Result<CampaignParticipantId?> ResolveCreditedParticipant(
        CampaignParticipantId? creditedParticipantId
    )
    {
        if (creditedParticipantId is not { IsEmpty: false } pId)
        {
            return Result.Success<CampaignParticipantId?>(value: null);
        }

        var participant = _participants.Find(p => p.Id == pId && p.IsFundraisingEnabled);
        return participant is null
            ? FinanceCampaignErrors.ParticipantNotFound
            : Result.Success<CampaignParticipantId?>(participant.Id);
    }

    private static Guid? NormalizePersonGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    private static Result ValidateSpec(
        FundraisingCampaignSpec spec,
        out string currency,
        out int scale
    )
    {
        currency = "USD";
        scale = 2;

        return !IsValidIdentity(spec) ? FinanceCampaignErrors.CampaignInvalid
            : !IsoCurrencyCatalog.TryGetScale(spec.CurrencyCode, out currency, out scale)
                ? FinanceErrors.CurrencyCodeInvalid
            : !IsValidGoalsAndDates(spec) ? FinanceCampaignErrors.CampaignInvalid
            : spec.TeamGoalOverrideAmount.HasValue && !spec.AllowTeamGoalOverride
                ? FinanceCampaignErrors.TeamGoalOverrideNotAllowed
            : Result.Success();
    }

    private static bool IsValidIdentity(FundraisingCampaignSpec spec)
    {
        var normalizedSlug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && CampaignSlugRegex.IsMatch(normalizedSlug)
            && spec.GroupId != Guid.Empty
            && spec.FinancialAccountId != Guid.Empty;
    }

    private static bool IsValidGoalsAndDates(FundraisingCampaignSpec spec)
    {
        var validDates =
            spec.StartDate is null || spec.EndDate is null || spec.EndDate >= spec.StartDate;
        var validPartGoal =
            !spec.DefaultParticipantGoalAmount.HasValue
            || spec.DefaultParticipantGoalAmount.Value > 0m;
        var validOverride =
            !spec.TeamGoalOverrideAmount.HasValue || spec.TeamGoalOverrideAmount.Value > 0m;
        return validDates && spec.CampaignGoalAmount > 0m && validPartGoal && validOverride;
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var (key, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributeValues[key.Trim()] = value;
            }
        }
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
    private static partial Regex CampaignSlugRegex { get; }
}
