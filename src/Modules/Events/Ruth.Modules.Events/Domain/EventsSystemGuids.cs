namespace Ruth.Modules.Events.Domain;

/// <summary>Deterministic system GUIDs for Events module entity type registrations.</summary>
public static class EventsSystemGuids
{
    public static readonly Guid EventCalendar = new(
        0x01966000,
        0x0701,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x07,
        0x01
    );

    public static readonly Guid EventItem = new(
        0x01966000,
        0x0702,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x07,
        0x02
    );

    public static readonly Guid InteractiveSession = new(
        0x01966000,
        0x0703,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x07,
        0x03
    );

    public static readonly Guid RegistrationTemplate = new(
        0x01966000,
        0x0801,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x08,
        0x04
    );

    public static readonly Guid RegistrationInstance = new(
        0x01966000,
        0x0802,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x08,
        0x05
    );

    public static readonly Guid Registration = new(
        0x01966000,
        0x0803,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x08,
        0x06
    );
}
