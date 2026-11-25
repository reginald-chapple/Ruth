using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.BackgroundChecks;

/// <summary>Background check screening aggregate with out-of-order webhook state guards.</summary>
public sealed class BackgroundCheck
    : AggregateRoot<BackgroundCheckId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for BackgroundCheck.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.BackgroundCheck";

    private BackgroundCheck() { }

    private BackgroundCheck(
        BackgroundCheckId id,
        PersonId personId,
        string providerKey,
        string packageName,
        string? requestId,
        BackgroundCheckStatus status,
        string rawProviderStatus,
        DateTimeOffset requestedOnUtc,
        Guid? requestedByUserId
    )
        : base(id)
    {
        PersonId = personId;
        ProviderKey = providerKey;
        PackageName = packageName;
        RequestId = requestId;
        Status = status;
        RawProviderStatus = rawProviderStatus;
        Recommendation = MapRecommendation(status);
        RequestedOnUtc = requestedOnUtc.ToUniversalTime();
        RequestedByUserId = requestedByUserId;
    }

    /// <summary>Gets the screened person identifier.</summary>
    public PersonId PersonId { get; private set; }

    /// <summary>Gets the modular provider key (e.g. "checkr", "in-memory").</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>Gets the screening package slug or name.</summary>
    public string PackageName { get; private set; } = string.Empty;

    /// <summary>Gets the provider request/candidate identifier used to correlate webhooks.</summary>
    public string? RequestId { get; private set; }

    /// <summary>Gets the provider response/document identifier.</summary>
    public string? ResponseId { get; private set; }

    /// <summary>Gets the optional Core BinaryFile identifier storing the PDF report.</summary>
    public Guid? ResponseDocumentBinaryFileId { get; private set; }

    /// <summary>Gets the UTC request timestamp.</summary>
    public DateTimeOffset RequestedOnUtc { get; private set; }

    /// <summary>Gets the optional UTC response completion timestamp.</summary>
    public DateTimeOffset? RespondedOnUtc { get; private set; }

    /// <summary>Gets the normalized background check lifecycle status.</summary>
    public BackgroundCheckStatus Status { get; private set; }

    /// <summary>Gets the raw status string reported by the provider.</summary>
    public string RawProviderStatus { get; private set; } = string.Empty;

    /// <summary>Gets whether an adverse record was found (true = consider, false = clear, null = pending).</summary>
    public bool? RecordFound { get; private set; }

    /// <summary>Gets the human-readable Rock-style recommendation string.</summary>
    public string Recommendation { get; private set; } = string.Empty;

    /// <summary>Gets the raw JSON payload of the most recent provider response.</summary>
    public string? ResponseDataJson { get; private set; }

    /// <summary>Gets the optional requesting user identifier.</summary>
    public Guid? RequestedByUserId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalRestrictedDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalRestrictedDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a new background check request record.</summary>
    public static BackgroundCheck Create(
        BackgroundCheckId id,
        PersonId personId,
        string providerKey,
        string packageName,
        string? requestId,
        BackgroundCheckStatus initialStatus,
        string rawProviderStatus,
        DateTimeOffset requestedOnUtc,
        Guid? requestedByUserId = null
    )
    {
        return new BackgroundCheck(
            id,
            personId,
            providerKey.Trim().ToLowerInvariant(),
            packageName.Trim(),
            requestId?.Trim(),
            initialStatus,
            rawProviderStatus.Trim(),
            requestedOnUtc,
            requestedByUserId
        );
    }

    /// <summary>Applies an inbound verified webhook event with Rock's out-of-order terminal status guard.</summary>
    public void ApplyWebhookEvent(BackgroundCheckWebhookEvent webhookEvent, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        var isAlreadyTerminal =
            Status is BackgroundCheckStatus.ReportClear or BackgroundCheckStatus.ReportConsider;
        var incomingIsPreReport =
            webhookEvent.MappedStatus
            is BackgroundCheckStatus.Requested
                or BackgroundCheckStatus.InvitationSent
                or BackgroundCheckStatus.InvitationCompleted
                or BackgroundCheckStatus.InvitationExpired
                or BackgroundCheckStatus.ReportPending;

        // Out-of-order guard: never downgrade a completed report back to an earlier invitation/pending state.
        if (isAlreadyTerminal && incomingIsPreReport)
        {
            return;
        }

        Status = webhookEvent.MappedStatus;
        RawProviderStatus = webhookEvent.EventType;
        Recommendation = string.IsNullOrWhiteSpace(webhookEvent.Recommendation)
            ? MapRecommendation(webhookEvent.MappedStatus)
            : webhookEvent.Recommendation;

        if (!string.IsNullOrWhiteSpace(webhookEvent.PackageName))
        {
            PackageName = webhookEvent.PackageName;
        }

        if (!string.IsNullOrWhiteSpace(webhookEvent.ProviderResponseId))
        {
            ResponseId = webhookEvent.ProviderResponseId;
        }

        if (webhookEvent.RecordFound.HasValue)
        {
            RecordFound = webhookEvent.RecordFound.Value;
        }

        ResponseDataJson = webhookEvent.RawPayloadJson;

        if (
            webhookEvent.MappedStatus
            is BackgroundCheckStatus.ReportClear
                or BackgroundCheckStatus.ReportConsider
        )
        {
            RespondedOnUtc = nowUtc.ToUniversalTime();
        }
    }

    internal void RepointPerson(PersonId targetPersonId)
    {
        PersonId = targetPersonId;
    }

    /// <summary>Maps a normalized status to Rock's recommendation text.</summary>
    public static string MapRecommendation(BackgroundCheckStatus status)
    {
        return status switch
        {
            BackgroundCheckStatus.InvitationSent => "Invitation Sent",
            BackgroundCheckStatus.InvitationCompleted => "Invitation Completed",
            BackgroundCheckStatus.InvitationExpired => "Invitation Expired",
            BackgroundCheckStatus.ReportPending => "Report Pending",
            BackgroundCheckStatus.ReportClear => "Candidate Pass",
            BackgroundCheckStatus.ReportConsider => "Candidate Review",
            BackgroundCheckStatus.ReportSuspended => "Report Suspended",
            BackgroundCheckStatus.Requested => "Requested",
            _ => "Requested",
        };
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
