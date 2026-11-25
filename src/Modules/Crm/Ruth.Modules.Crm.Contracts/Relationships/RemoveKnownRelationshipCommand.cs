using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Relationships;

/// <summary>Command to remove a known relationship and its linked inverse relationship.</summary>
public sealed record RemoveKnownRelationshipCommand(Guid RelationshipId) : ICommand<bool>;
