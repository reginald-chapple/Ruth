namespace Ruth.Modules.Groups.Tests.Fixtures;

internal sealed record ServingSetupContext(
    Guid GroupTypeId,
    Guid GroupId,
    Guid LocationId,
    Guid ScheduleId,
    Guid AlicePersonId,
    Guid BobPersonId,
    Guid UnreadyPersonId
);
