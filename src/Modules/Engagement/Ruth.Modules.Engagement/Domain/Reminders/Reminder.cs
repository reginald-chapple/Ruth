using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain.Reminders;

/// <summary>Entity follow-up Reminder supporting one-shot completion or cyclic renewal.</summary>
public sealed class Reminder : AggregateRoot<ReminderId>, IAuditableEntity, IHasConcurrencyToken
{
    private Reminder() { }

    private Reminder(ReminderId id)
        : base(id) { }

    public Guid ReminderTypeId { get; private set; }
    public Guid AssigneePersonId { get; private set; }
    public string TargetEntityTypeKey { get; private set; } = string.Empty;
    public Guid TargetEntityId { get; private set; }
    public string EntitySummary { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public DateOnly ReminderDate { get; private set; }
    public int? RenewPeriodDays { get; private set; }
    public int? RenewMaxCount { get; private set; }
    public int RenewCurrentCount { get; private set; }
    public bool IsComplete { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? LastNotifiedAtUtc { get; private set; }

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

    public static Result<Reminder> Create(
        ReminderType reminderType,
        Guid assigneePersonId,
        string entityTypeKey,
        Guid entityId,
        string entitySummary,
        string note,
        DateOnly reminderDate,
        int? renewPeriodDays,
        int? renewMaxCount
    )
    {
        ArgumentNullException.ThrowIfNull(reminderType);
        if (
            !HasValidCoreFields(assigneePersonId, entityTypeKey, entityId, note)
            || !HasValidRenewalOptions(renewPeriodDays, renewMaxCount)
            || !MatchesEntityFilter(reminderType.EntityTypeKeyFilter, entityTypeKey)
        )
        {
            return EngagementErrors.ReminderInvalid;
        }

        var trimmedKey = entityTypeKey.Trim();
        return new Reminder(ReminderId.Create())
        {
            ReminderTypeId = reminderType.Id.Value,
            AssigneePersonId = assigneePersonId,
            TargetEntityTypeKey = trimmedKey,
            TargetEntityId = entityId,
            EntitySummary = string.IsNullOrWhiteSpace(entitySummary)
                ? trimmedKey
                : entitySummary.Trim(),
            Note = note.Trim(),
            ReminderDate = reminderDate,
            RenewPeriodDays = renewPeriodDays,
            RenewMaxCount = renewMaxCount,
        };
    }

    public void MarkComplete(DateTimeOffset nowUtc)
    {
        IsComplete = true;
        CompletedAtUtc = nowUtc.ToUniversalTime();
    }

    public bool RecordNotificationAndAdvance(
        ReminderType reminderType,
        DateOnly today,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(reminderType);
        if (!IsDueForNotificationToday(today))
        {
            return false;
        }

        LastNotifiedAtUtc = nowUtc.ToUniversalTime();
        AdvanceRenewalOrComplete(reminderType.ShouldAutoCompleteWhenNotified, nowUtc);
        return true;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (AssigneePersonId != retiredPersonId)
        {
            return false;
        }

        AssigneePersonId = survivingPersonId;
        return true;
    }

    private bool IsDueForNotificationToday(DateOnly today)
    {
        return !IsComplete
            && ReminderDate <= today
            && (
                !LastNotifiedAtUtc.HasValue
                || DateOnly.FromDateTime(LastNotifiedAtUtc.Value.UtcDateTime) != today
            );
    }

    private void AdvanceRenewalOrComplete(bool autoComplete, DateTimeOffset nowUtc)
    {
        if (
            RenewPeriodDays is > 0
            && (!RenewMaxCount.HasValue || RenewCurrentCount < RenewMaxCount.Value)
        )
        {
            RenewCurrentCount++;
            ReminderDate = ReminderDate.AddDays(RenewPeriodDays.Value);
        }
        else if (autoComplete)
        {
            MarkComplete(nowUtc);
        }
    }

    private static bool HasValidCoreFields(
        Guid assigneePersonId,
        string entityTypeKey,
        Guid entityId,
        string note
    )
    {
        return assigneePersonId != Guid.Empty
            && !string.IsNullOrWhiteSpace(entityTypeKey)
            && entityId != Guid.Empty
            && !string.IsNullOrWhiteSpace(note);
    }

    private static bool HasValidRenewalOptions(int? renewPeriodDays, int? renewMaxCount)
    {
        return (!renewPeriodDays.HasValue || renewPeriodDays.Value >= 1)
            && (!renewMaxCount.HasValue || renewMaxCount.Value >= 1);
    }

    private static bool MatchesEntityFilter(string? filter, string entityTypeKey)
    {
        return string.IsNullOrWhiteSpace(filter)
            || string.Equals(filter, entityTypeKey.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
