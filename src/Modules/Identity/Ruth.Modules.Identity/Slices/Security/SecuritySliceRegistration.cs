using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Identity.Slices.Security;

internal static class SecuritySliceRegistration
{
    public static IServiceCollection AddSecurityHandlers(this IServiceCollection services)
    {
        RegisterRoleAndAclHandlers(services);
        RegisterApiKeyAndOAuthHandlers(services);
        return services;
    }

    private static void RegisterRoleAndAclHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateSecurityRoleCommand,
            SecurityRoleResponse,
            CreateSecurityRoleCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateSecurityRoleCommand,
            SecurityRoleResponse,
            UpdateSecurityRoleCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteSecurityRoleCommand,
            bool,
            DeleteSecurityRoleCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSecurityRolesQuery,
            IReadOnlyList<SecurityRoleResponse>,
            GetSecurityRolesQueryHandler
        >();
        _ = services.AddCommandHandler<AssignUserRoleCommand, bool, AssignUserRoleCommandHandler>();
        _ = services.AddCommandHandler<RemoveUserRoleCommand, bool, RemoveUserRoleCommandHandler>();
        _ = services.AddQueryHandler<
            GetEntityAclRulesQuery,
            IReadOnlyList<EntityAclRuleResponse>,
            GetEntityAclRulesQueryHandler
        >();
        _ = services.AddCommandHandler<
            SaveEntityAclRulesCommand,
            IReadOnlyList<EntityAclRuleResponse>,
            SaveEntityAclRulesCommandHandler
        >();
        _ = services.AddQueryHandler<
            VerifyEntityAuthorizationQuery,
            AclVerificationResponse,
            VerifyEntityAuthorizationQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAuthAuditLogsQuery,
            IReadOnlyList<AuthAuditLogResponse>,
            GetAuthAuditLogsQueryHandler
        >();
    }

    private static void RegisterApiKeyAndOAuthHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateRestApiKeyCommand,
            CreateRestApiKeyResponse,
            CreateRestApiKeyCommandHandler
        >();
        _ = services.AddCommandHandler<
            RevokeRestApiKeyCommand,
            bool,
            RevokeRestApiKeyCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRestApiKeysQuery,
            IReadOnlyList<RestApiKeyResponse>,
            GetRestApiKeysQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAuthClientCommand,
            AuthClientResponse,
            CreateAuthClientCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAuthClientsQuery,
            IReadOnlyList<AuthClientResponse>,
            GetAuthClientsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAuthScopeCommand,
            AuthScopeResponse,
            CreateAuthScopeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAuthScopesQuery,
            IReadOnlyList<AuthScopeResponse>,
            GetAuthScopesQueryHandler
        >();
    }
}
