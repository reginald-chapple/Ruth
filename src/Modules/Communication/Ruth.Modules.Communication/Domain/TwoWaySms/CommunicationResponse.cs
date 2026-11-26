using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Two-way SMS conversation message aggregate (comms.communication_responses).</summary>
public sealed class CommunicationResponse
    : AggregateRoot<CommunicationResponseId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type key.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.CommunicationResponse";

    private CommunicationResponse() { }

    private CommunicationResponse(
        CommunicationResponseId id,
        Guid systemPhoneNumberId,
        string systemPhoneNumber,
        string participantPhoneNumber,
        Guid? participantPersonId,
        string participantDisplayName,
        SmsMessageDirection direction,
        string messageBody,
        string? matchedKeyword,
        string? providerMessageId,
        bool isRead,
        Guid? repliedByPersonId,
        DateTimeOffset occurredAtUtc
    )
        : base(id)
    {
        SystemPhoneNumberId = systemPhoneNumberId;
        SystemPhoneNumber = systemPhoneNumber;
        ParticipantPhoneNumber = participantPhoneNumber;
        ParticipantPersonId =
            participantPersonId.HasValue && participantPersonId.Value != Guid.Empty
                ? participantPersonId
                : null;
        ParticipantDisplayName = participantDisplayName;
        Direction = direction;
        MessageBody = messageBody;
        MatchedKeyword = matchedKeyword;
        ProviderMessageId = providerMessageId;
        IsRead = isRead;
        RepliedByPersonId =
            repliedByPersonId.HasValue && repliedByPersonId.Value != Guid.Empty
                ? repliedByPersonId
                : null;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public Guid SystemPhoneNumberId { get; private set; }
    public string SystemPhoneNumber { get; private set; } = string.Empty;
    public string ParticipantPhoneNumber { get; private set; } = string.Empty;
    public Guid? ParticipantPersonId { get; private set; }
    public string ParticipantDisplayName { get; private set; } = string.Empty;
    public SmsMessageDirection Direction { get; private set; }
    public string MessageBody { get; private set; } = string.Empty;
    public string? MatchedKeyword { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public bool IsRead { get; private set; }
    public Guid? RepliedByPersonId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

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

    /// <summary>Creates a validated two-way SMS <see cref="CommunicationResponse" /> entry.</summary>
    public static Result<CommunicationResponse> Create(
        Guid systemPhoneNumberId,
        string systemPhoneNumber,
        string? rawParticipantPhone,
        SmsMessageDirection direction,
        string? messageBody,
        DateTimeOffset occurredAtUtc,
        Guid? participantPersonId = null,
        string? participantDisplayName = null,
        string? matchedKeyword = null,
        string? providerMessageId = null,
        Guid? repliedByPersonId = null
    )
    {
        var participantE164 = PhoneNumbers.SystemPhoneNumber.NormalizeE164(rawParticipantPhone);
        if (participantE164.IsFailure)
        {
            return Result.Failure<CommunicationResponse>(participantE164.Errors);
        }

        if (string.IsNullOrWhiteSpace(messageBody) || messageBody.Trim().Length > 1_600)
        {
            return Result.Failure<CommunicationResponse>(
                BulkCommunicationErrors.SmsMessageBodyInvalid
            );
        }

        var display = string.IsNullOrWhiteSpace(participantDisplayName)
            ? participantE164.Value
            : participantDisplayName.Trim();
        var isRead = direction != SmsMessageDirection.Inbound;

        return new CommunicationResponse(
            CommunicationResponseId.Create(),
            systemPhoneNumberId,
            systemPhoneNumber,
            participantE164.Value,
            participantPersonId,
            display,
            direction,
            messageBody.Trim(),
            Clean(matchedKeyword),
            Clean(providerMessageId),
            isRead,
            repliedByPersonId,
            occurredAtUtc
        );
    }

    public void MarkRead()
    {
        IsRead = true;
    }

    public void SetMatchedKeyword(string? keyword)
    {
        MatchedKeyword = Clean(keyword);
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ParticipantPersonId == retiredPersonId)
        {
            ParticipantPersonId = survivingPersonId;
        }

        if (RepliedByPersonId == retiredPersonId)
        {
            RepliedByPersonId = survivingPersonId;
        }
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
