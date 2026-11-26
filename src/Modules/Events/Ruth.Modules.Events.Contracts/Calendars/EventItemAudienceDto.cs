namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Target audience tag attached to an event item.</summary>
public sealed record EventItemAudienceDto(Guid? AudienceDefinedValueId, string AudienceLabel);
