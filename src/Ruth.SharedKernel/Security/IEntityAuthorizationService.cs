namespace Ruth.SharedKernel.Security;

/// <summary>Evaluates hierarchical Allow/Deny entity ACL rules for a subject and action.</summary>
public interface IEntityAuthorizationService
{
    /// <summary>Determines whether <paramref name="subject" /> is authorized to perform <paramref name="action" /> on <paramref name="entity" />.</summary>
    ValueTask<bool> IsAuthorizedAsync(
        ISecured entity,
        string action,
        SecuritySubject subject,
        CancellationToken cancellationToken = default
    );
}
