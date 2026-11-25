using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Retrieves recent authorization rule change audit entries.</summary>
public sealed record GetAuthAuditLogsQuery(
    string? EntityTypeKey = null,
    Guid? EntityId = null,
    int MaxCount = 100
) : IQuery<IReadOnlyList<AuthAuditLogResponse>>;
