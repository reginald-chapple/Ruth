using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Automated multi-step drip campaign aggregate with exit and conversion rules.</summary>
public sealed partial class CommunicationFlow
    : AggregateRoot<CommunicationFlowId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.CommunicationFlow";

    private List<CommunicationFlowStep> _steps = [];

    private CommunicationFlow() { }

    private CommunicationFlow(CommunicationFlowId id, Guid systemGuid, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        IsSystem = isSystem;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CommunicationFlowCategory Category { get; private set; }
    public string TopicKey { get; private set; } = "general-announcements";
    public FlowTriggerMode TriggerMode { get; private set; }
    public FlowExitCondition ExitCondition { get; private set; }
    public FlowConversionGoalKind ConversionGoalKind { get; private set; }
    public Guid? ConversionTargetGroupId { get; private set; }
    public string? ConversionGoalUrlPrefix { get; private set; }
    public int ConversionWindowDays { get; private set; } = 30;
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<CommunicationFlowStep> Steps => _steps;

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

    /// <summary>Creates a validated <see cref="CommunicationFlow" /> aggregate.</summary>
    public static Result<CommunicationFlow> Create(CommunicationFlowSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Key) || !SlugRegex.IsMatch(spec.Key.Trim()))
        {
            return Result.Failure<CommunicationFlow>(CommunicationFlowErrors.FlowKeyInvalid);
        }

        var id = CommunicationFlowId.Create();
        var flow = new CommunicationFlow(id, spec.SystemGuid ?? id.Value, spec.IsSystem)
        {
            Key = spec.Key.Trim(),
        };

        var updated = flow.Update(spec);
        return updated.IsFailure ? Result.Failure<CommunicationFlow>(updated.Errors) : flow;
    }

    /// <summary>Updates mutable fields and timed steps on this flow.</summary>
    public Result Update(CommunicationFlowSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Name) || spec.Name.Trim().Length > 150)
        {
            return CommunicationFlowErrors.FlowNameInvalid;
        }

        if (string.IsNullOrWhiteSpace(spec.TopicKey) || !TopicRegex.IsMatch(spec.TopicKey.Trim()))
        {
            return BulkCommunicationErrors.TopicKeyInvalid;
        }

        if (!ValidateConversionSettings(spec))
        {
            return CommunicationFlowErrors.FlowConversionConfigInvalid;
        }

        var stepsResult = CommunicationFlowStep.ValidateAndNormalize(spec.Steps);
        if (stepsResult.IsFailure)
        {
            return Result.Failure(stepsResult.Errors);
        }

        ApplySpec(spec, stepsResult.Value);
        return Result.Success();
    }

    public Result EnsureCanDelete()
    {
        return IsSystem ? CommunicationFlowErrors.SystemFlowProtected : Result.Success();
    }

    private static bool ValidateConversionSettings(CommunicationFlowSpec spec)
    {
        return spec.ConversionWindowDays is >= 1 and <= 365
            && spec.ConversionGoalKind switch
            {
                FlowConversionGoalKind.JoinedTargetGroup => spec.ConversionTargetGroupId.HasValue
                    && spec.ConversionTargetGroupId.Value != Guid.Empty,
                FlowConversionGoalKind.ClickedGoalUrl => !string.IsNullOrWhiteSpace(
                    spec.ConversionGoalUrlPrefix
                ),
                FlowConversionGoalKind.None
                or FlowConversionGoalKind.AttendedSession
                or FlowConversionGoalKind.ManualOrExternalGoal
                or _ => true,
            };
    }

    private void ApplySpec(CommunicationFlowSpec spec, IReadOnlyList<CommunicationFlowStep> steps)
    {
        Name = spec.Name.Trim();
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim();
        Category = spec.Category;
        TopicKey = spec.TopicKey.Trim();
        TriggerMode = spec.TriggerMode;
        ExitCondition = spec.ExitCondition;
        ConversionGoalKind = spec.ConversionGoalKind;
        ConversionTargetGroupId =
            spec.ConversionTargetGroupId.HasValue
            && spec.ConversionTargetGroupId.Value != Guid.Empty
                ? spec.ConversionTargetGroupId
                : null;
        ConversionGoalUrlPrefix = string.IsNullOrWhiteSpace(spec.ConversionGoalUrlPrefix)
            ? null
            : spec.ConversionGoalUrlPrefix.Trim();
        ConversionWindowDays = spec.ConversionWindowDays;
        IsActive = spec.IsActive;
        _steps = [.. steps];
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{2,99}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }

    [GeneratedRegex(
        @"^[a-z][a-z0-9._-]{1,79}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex TopicRegex { get; }
}
