namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Represents a public or internal contact topic configured for a campus.</summary>
public sealed record CampusTopicDto(Guid TopicTypeValueId, string Email, bool IsPublic);
