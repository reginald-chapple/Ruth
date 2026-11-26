using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Reminders;

/// <summary>In-app Notification Bell message for staff and constituents.</summary>
public sealed class NotificationMessage
    : AggregateRoot<NotificationMessageId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private NotificationMessage() { }

    private NotificationMessage(NotificationMessageId id)
        : base(id) { }

    public Guid RecipientPersonId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationCategory Category { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? ColorHex { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

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

    public static NotificationMessage Create(
        Guid recipientPersonId,
        string title,
        string message,
        NotificationCategory category,
        string? actionUrl = null,
        string? colorHex = null
    )
    {
        return new NotificationMessage(NotificationMessageId.Create())
        {
            RecipientPersonId = recipientPersonId,
            Title = title.Trim(),
            Message = message.Trim(),
            Category = category,
            ActionUrl = TrimOptional(actionUrl, 500),
            ColorHex = TrimOptional(colorHex, 16),
        };
    }

    public void MarkRead(DateTimeOffset nowUtc)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = nowUtc.ToUniversalTime();
        }
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (RecipientPersonId != retiredPersonId)
        {
            return false;
        }

        RecipientPersonId = survivingPersonId;
        return true;
    }
}
