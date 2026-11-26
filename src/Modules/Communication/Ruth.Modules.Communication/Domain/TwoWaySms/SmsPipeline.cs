using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Ordered inbound SMS keyword responder pipeline aggregate.</summary>
public sealed class SmsPipeline
    : AggregateRoot<SmsPipelineId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.SmsPipeline";

    private readonly List<SmsAction> _actions = [];

    private SmsPipeline() { }

    private SmsPipeline(SmsPipelineId id, Guid systemGuid, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        IsSystem = isSystem;
    }

    public Guid SystemGuid { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? SystemPhoneNumberId { get; private set; }
    public int PriorityOrder { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<SmsAction> Actions => _actions;

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

    /// <summary>Creates a validated <see cref="SmsPipeline" /> aggregate with ordered actions.</summary>
    public static Result<SmsPipeline> Create(
        string name,
        IReadOnlyList<SmsActionInputDto>? actions,
        string? description = null,
        Guid? systemPhoneNumberId = null,
        int priorityOrder = 10,
        bool isActive = true,
        bool isSystem = false,
        Guid? systemGuid = null
    )
    {
        var id = SmsPipelineId.Create();
        var pipeline = new SmsPipeline(id, systemGuid ?? id.Value, isSystem);
        var updated = pipeline.Update(
            name,
            actions,
            description,
            systemPhoneNumberId,
            priorityOrder,
            isActive
        );
        return updated.IsFailure ? Result.Failure<SmsPipeline>(updated.Errors) : pipeline;
    }

    /// <summary>Updates properties and replaces ordered <see cref="SmsAction" /> entries.</summary>
    public Result Update(
        string name,
        IReadOnlyList<SmsActionInputDto>? actions,
        string? description,
        Guid? systemPhoneNumberId,
        int priorityOrder,
        bool isActive
    )
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            return BulkCommunicationErrors.SmsPipelineNameInvalid;
        }

        var validatedResult = BuildValidatedActions(Id, actions);
        if (validatedResult.IsFailure)
        {
            return Result.Failure(validatedResult.Errors);
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SystemPhoneNumberId =
            systemPhoneNumberId.HasValue && systemPhoneNumberId.Value != Guid.Empty
                ? systemPhoneNumberId
                : null;
        PriorityOrder = Math.Clamp(priorityOrder, 0, 10_000);
        IsActive = isActive;
        _actions.Clear();
        _actions.AddRange(validatedResult.Value);
        return Result.Success();
    }

    private static Result<List<SmsAction>> BuildValidatedActions(
        SmsPipelineId pipelineId,
        IReadOnlyList<SmsActionInputDto>? actions
    )
    {
        if (actions is null || actions.Count is 0 or > 30)
        {
            return Result.Failure<List<SmsAction>>(
                BulkCommunicationErrors.SmsPipelineActionsInvalid
            );
        }

        var validated = new List<SmsAction>(actions.Count);
        foreach (var dto in actions.OrderBy(static a => a.Order))
        {
            var actionResult = SmsAction.Create(pipelineId, dto, validated.Count);
            if (actionResult.IsFailure)
            {
                return Result.Failure<List<SmsAction>>(actionResult.Errors);
            }

            validated.Add(actionResult.Value);
        }

        return validated;
    }

    /// <summary>Checks whether this pipeline can be deleted.</summary>
    public Result EnsureCanDelete()
    {
        return IsSystem ? BulkCommunicationErrors.SystemSmsPipelineProtected : Result.Success();
    }
}
