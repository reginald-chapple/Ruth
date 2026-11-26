using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Per-recipient delivery, merge context, and webhook telemetry record within a <see cref="Communication" />.</summary>
public sealed class CommunicationRecipient
    : Entity<CommunicationRecipientId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private Dictionary<string, string?> _mergeFields = new(StringComparer.OrdinalIgnoreCase);

    private CommunicationRecipient() { }

    private CommunicationRecipient(
        CommunicationRecipientId id,
        CommunicationId communicationId,
        Guid? personId,
        string destinationAddress,
        string normalizedDestination,
        string? displayName,
        Dictionary<string, string?> mergeFields
    )
        : base(id)
    {
        CommunicationId = communicationId;
        PersonId = personId.HasValue && personId.Value != Guid.Empty ? personId : null;
        DestinationAddress = destinationAddress;
        NormalizedDestination = normalizedDestination;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Status = RecipientDeliveryStatus.Pending;
        BounceKind = BounceKind.None;
        _mergeFields = mergeFields;
    }

    public CommunicationId CommunicationId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string DestinationAddress { get; private set; } = string.Empty;
    public string NormalizedDestination { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public RecipientDeliveryStatus Status { get; private set; }
    public string? TransportKey { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public BounceKind BounceKind { get; private set; }
    public int OpenedCount { get; private set; }
    public DateTimeOffset? FirstOpenedAtUtc { get; private set; }
    public int ClickedCount { get; private set; }
    public DateTimeOffset? FirstClickedAtUtc { get; private set; }
    public string? LastClickedUrl { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public string? StatusNote { get; private set; }

    public IReadOnlyDictionary<string, string?> MergeFields => _mergeFields;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    /// <summary>Creates a validated <see cref="CommunicationRecipient" /> normalized for the target channel.</summary>
    public static Result<CommunicationRecipient> Create(
        CommunicationId communicationId,
        CommunicationChannel channel,
        string? rawDestination,
        Guid? personId = null,
        string? displayName = null,
        IReadOnlyDictionary<string, string?>? mergeFields = null
    )
    {
        var normalizedResult = NormalizeDestination(channel, rawDestination);
        if (normalizedResult.IsFailure)
        {
            return Result.Failure<CommunicationRecipient>(normalizedResult.Errors);
        }

        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (mergeFields is not null)
        {
            foreach (var kvp in mergeFields)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    fields[kvp.Key.Trim()] = kvp.Value;
                }
            }
        }

        return new CommunicationRecipient(
            CommunicationRecipientId.Create(),
            communicationId,
            personId,
            normalizedResult.Value.Display,
            normalizedResult.Value.Normalized,
            displayName,
            fields
        );
    }

    /// <summary>Normalizes a channel destination address for deduplication and suppression lookups.</summary>
    public static Result<(string Display, string Normalized)> NormalizeDestination(
        CommunicationChannel channel,
        string? rawDestination
    )
    {
        if (string.IsNullOrWhiteSpace(rawDestination))
        {
            return Result.Failure<(string, string)>(
                BulkCommunicationErrors.RecipientDestinationInvalid
            );
        }

        var trimmed = rawDestination.Trim();
        return channel switch
        {
            CommunicationChannel.Email => NormalizeEmailAddress(trimmed),
            CommunicationChannel.Sms => NormalizeSmsNumber(trimmed),
            CommunicationChannel.Push or _ => trimmed.Length < 8
                ? Result.Failure<(string, string)>(
                    BulkCommunicationErrors.RecipientDestinationInvalid
                )
                : (trimmed, trimmed),
        };
    }

    private static Result<(string Display, string Normalized)> NormalizeEmailAddress(string trimmed)
    {
        var atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);
        return
            atIndex <= 0
            || atIndex >= trimmed.Length - 2
            || trimmed.Contains(' ', StringComparison.Ordinal)
            ? Result.Failure<(string, string)>(BulkCommunicationErrors.RecipientDestinationInvalid)
            : (trimmed, trimmed.ToLowerInvariant());
    }

    private static Result<(string Display, string Normalized)> NormalizeSmsNumber(string trimmed)
    {
        var e164 = SystemPhoneNumber.NormalizeE164(trimmed);
        return e164.IsFailure
            ? Result.Failure<(string, string)>(BulkCommunicationErrors.RecipientDestinationInvalid)
            : (e164.Value, e164.Value);
    }

    public void MarkDelivered(
        string transportKey,
        string? providerMessageId,
        DateTimeOffset sentAtUtc
    )
    {
        Status = RecipientDeliveryStatus.Delivered;
        TransportKey = transportKey;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId)
            ? null
            : providerMessageId.Trim();
        SentAtUtc = sentAtUtc.ToUniversalTime();
        StatusNote = null;
    }

    public void MarkFailed(string transportKey, string errorDetail, DateTimeOffset attemptedAtUtc)
    {
        Status = RecipientDeliveryStatus.Failed;
        TransportKey = transportKey;
        SentAtUtc = attemptedAtUtc.ToUniversalTime();
        StatusNote = Truncate(errorDetail, 500);
    }

    public void MarkSkippedUnsubscribed(string reason, DateTimeOffset checkedAtUtc)
    {
        Status = RecipientDeliveryStatus.SkippedUnsubscribed;
        SentAtUtc = checkedAtUtc.ToUniversalTime();
        StatusNote = Truncate(reason, 300);
    }

    public void RecordOpen(DateTimeOffset openedAtUtc)
    {
        OpenedCount++;
        FirstOpenedAtUtc ??= openedAtUtc.ToUniversalTime();
    }

    public void RecordClick(string? clickedUrl, DateTimeOffset clickedAtUtc)
    {
        ClickedCount++;
        FirstClickedAtUtc ??= clickedAtUtc.ToUniversalTime();
        if (!string.IsNullOrWhiteSpace(clickedUrl))
        {
            LastClickedUrl = Truncate(clickedUrl.Trim(), 1000);
        }
    }

    public void RecordBounce(BounceKind bounceKind, string? reason)
    {
        Status = RecipientDeliveryStatus.Bounced;
        BounceKind = bounceKind == BounceKind.None ? BounceKind.Hard : bounceKind;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            StatusNote = Truncate(reason.Trim(), 500);
        }
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    internal void ReplaceMergeFieldsFromPersistence(Dictionary<string, string?> fields)
    {
        _mergeFields = fields;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
