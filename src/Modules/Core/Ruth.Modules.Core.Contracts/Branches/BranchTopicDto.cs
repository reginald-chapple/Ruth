namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Represents a public or internal contact topic configured for a branch or site.</summary>
public sealed record BranchTopicDto(Guid TopicTypeValueId, string Email, bool IsPublic);
