using Microsoft.AspNetCore.Authorization;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Infrastructure.Security;

internal sealed class EntityAuthorizationHandler(IEntityAuthorizationService authorizationService)
    : AuthorizationHandler<EntityPermissionRequirement, ISecured>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EntityPermissionRequirement requirement,
        ISecured resource
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(resource);

        var subject = context.User.ToSecuritySubject();
        if (await authorizationService.IsAuthorizedAsync(resource, requirement.Action, subject))
        {
            context.Succeed(requirement);
        }
    }
}
