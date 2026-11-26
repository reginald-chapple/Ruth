using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Events;

internal static class EventsEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            EventsSystemGuids.EventCalendar,
            EventCalendar.EntityTypeKeyName,
            nameof(EventCalendar),
            "Event Calendar",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EventsSystemGuids.EventItem,
            EventItem.EntityTypeKeyName,
            nameof(EventItem),
            "Event Item",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EventsSystemGuids.InteractiveSession,
            InteractiveSession.EntityTypeKeyName,
            nameof(InteractiveSession),
            "Interactive Session",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EventsSystemGuids.RegistrationTemplate,
            RegistrationTemplate.EntityTypeKeyName,
            nameof(RegistrationTemplate),
            "Registration Template",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EventsSystemGuids.RegistrationInstance,
            RegistrationInstance.EntityTypeKeyName,
            nameof(RegistrationInstance),
            "Registration Instance",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EventsSystemGuids.Registration,
            Registration.EntityTypeKeyName,
            nameof(Registration),
            "Event Registration",
            isSecured: true,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Events",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
