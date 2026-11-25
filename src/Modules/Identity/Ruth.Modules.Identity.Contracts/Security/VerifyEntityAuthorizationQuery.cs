using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Evaluates and explains whether a user (or anonymous caller) is authorized to perform an action on an entity.</summary>
public sealed record VerifyEntityAuthorizationQuery(
    string EntityTypeKey,
    Guid EntityId,
    string Action,
    Guid? UserAccountId = null,
    string? ParentEntityTypeKey = null,
    Guid? ParentEntityId = null
) : IQuery<AclVerificationResponse>;
