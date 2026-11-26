using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Tests.Fixtures;

internal sealed class StubEntityAuthorizationService : IEntityAuthorizationService
{
    private readonly HashSet<(string EntityTypeKey, Guid EntityId, Guid UserId)> _explicitAllows =
    [];
    private readonly HashSet<(string EntityTypeKey, Guid EntityId, Guid UserId)> _explicitDenies =
    [];

    public void Allow(string entityTypeKey, Guid entityId, Guid userId)
    {
        _ = _explicitAllows.Add((entityTypeKey, entityId, userId));
    }

    public void Deny(string entityTypeKey, Guid entityId, Guid userId)
    {
        _ = _explicitDenies.Add((entityTypeKey, entityId, userId));
    }

    public ValueTask<bool> IsAuthorizedAsync(
        ISecured entity,
        string action,
        SecuritySubject subject,
        CancellationToken cancellationToken = default
    )
    {
        if (subject.UserAccountId is { } uid)
        {
            if (_explicitDenies.Contains((entity.EntityTypeKey, entity.EntityId, uid)))
            {
                return ValueTask.FromResult(false);
            }

            if (_explicitAllows.Contains((entity.EntityTypeKey, entity.EntityId, uid)))
            {
                return ValueTask.FromResult(true);
            }
        }

        return ValueTask.FromResult(entity.IsAllowedByDefault(action));
    }
}
