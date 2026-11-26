using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Reminders;

/// <summary>Configurable ReminderType for one-shot or recurring entity follow-up reminders.</summary>
public sealed class ReminderType
    : AggregateRoot<ReminderTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.ReminderType";

    private ReminderType() { }

    private ReminderType(ReminderTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? EntityTypeKeyFilter { get; private set; }
    public string HighlightColorHex { get; private set; } = "#0F6CBD";
    public bool ShouldAutoCompleteWhenNotified { get; private set; }
    public ReminderNotificationFlavor NotificationFlavor { get; private set; }
    public string? SystemCommunicationKey { get; private set; }
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

    public static Result<ReminderType> Create(ReminderTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return
            string.IsNullOrWhiteSpace(spec.Name)
            || string.IsNullOrWhiteSpace(spec.HighlightColorHex)
            ? EngagementErrors.ReminderTypeInvalid
            : new ReminderType(ReminderTypeId.Create())
            {
                Name = spec.Name.Trim(),
                Description = TrimOptional(spec.Description, 500),
                EntityTypeKeyFilter = TrimOptional(spec.EntityTypeKey, 150),
                HighlightColorHex = spec.HighlightColorHex.Trim(),
                ShouldAutoCompleteWhenNotified = spec.ShouldAutoCompleteWhenNotified,
                NotificationFlavor = spec.NotificationFlavor,
                SystemCommunicationKey = TrimOptional(spec.SystemCommunicationKey, 120),
                IsActive = spec.IsActive,
            };
    }
}
