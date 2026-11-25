using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class EntityAuthorizationService(IAclSnapshotCache snapshotCache)
    : IEntityAuthorizationService
{
    public async ValueTask<bool> IsAuthorizedAsync(
        ISecured entity,
        string action,
        SecuritySubject subject,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(subject);

        var snapshot = await snapshotCache.GetSnapshotAsync(cancellationToken);
        return AclRuleEvaluator.IsAuthorized(snapshot, entity, action, subject);
    }

    public async ValueTask<AclEvaluationTrace> ExplainAsync(
        ISecured entity,
        string action,
        SecuritySubject subject,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(subject);

        var snapshot = await snapshotCache.GetSnapshotAsync(cancellationToken);
        return AclRuleEvaluator.Evaluate(snapshot, entity, action, subject);
    }
}
