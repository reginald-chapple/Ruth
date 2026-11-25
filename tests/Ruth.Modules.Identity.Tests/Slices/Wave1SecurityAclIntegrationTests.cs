using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Slices;

[Collection(IdentityPostgresFixtures.Name)]
public sealed class Wave1SecurityAclIntegrationTests(Postgres18ContainerFixture fixture)
{
    [Fact]
    public async Task Roles_AclRules_AuditLog_AndVerificationTrace_WorkEndToEnd()
    {
        var connectionString = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            connectionString,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var onboard = await commands.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(IdentityTestHostFactory.CreateValidCommand());
        Assert.True(onboard.IsSuccess);

        var adminEditCheck = await queries.QueryAsync<
            VerifyEntityAuthorizationQuery,
            AclVerificationResponse
        >(
            new VerifyEntityAuthorizationQuery(
                "Ruth.Core.DefinedType",
                Guid.CreateVersion7(),
                AuthorizationActions.Edit,
                onboard.Value.AdminUserId
            )
        );
        Assert.True(adminEditCheck.IsSuccess);
        Assert.True(adminEditCheck.Value.IsAllowed);
        Assert.Equal("InheritedRule", adminEditCheck.Value.Source);

        var role = await commands.SendAsync<CreateSecurityRoleCommand, SecurityRoleResponse>(
            new CreateSecurityRoleCommand("Ministry Staff", "Staff editors")
        );
        Assert.True(role.IsSuccess);

        var staff = await commands.SendAsync<CreateUserAccountCommand, UserAccountSummaryResponse>(
            new CreateUserAccountCommand("editor", "editor@grace.org", "Editor", "EditorPass#2026!")
        );
        Assert.True(staff.IsSuccess);

        var assigned = await commands.SendAsync<AssignUserRoleCommand, bool>(
            new AssignUserRoleCommand(staff.Value.UserAccountId, role.Value.Id)
        );
        Assert.True(assigned.IsSuccess);

        var parentId = Guid.CreateVersion7();
        var childId = Guid.CreateVersion7();

        var saveParentRules = await commands.SendAsync<
            SaveEntityAclRulesCommand,
            IReadOnlyList<EntityAclRuleResponse>
        >(
            new SaveEntityAclRulesCommand(
                "Ruth.Core.DefinedType",
                parentId,
                AuthorizationActions.Edit,
                [
                    new AclRuleInput(
                        AclRuleDecision.Allow,
                        SpecialRoleType.None,
                        SecurityRoleId: role.Value.Id
                    ),
                ],
                onboard.Value.AdminUserId
            )
        );
        Assert.True(saveParentRules.IsSuccess);

        var childCheck = await queries.QueryAsync<
            VerifyEntityAuthorizationQuery,
            AclVerificationResponse
        >(
            new VerifyEntityAuthorizationQuery(
                "Ruth.Core.DefinedValue",
                childId,
                AuthorizationActions.Edit,
                staff.Value.UserAccountId,
                ParentEntityTypeKey: "Ruth.Core.DefinedType",
                ParentEntityId: parentId
            )
        );
        Assert.True(childCheck.IsSuccess);
        Assert.True(childCheck.Value.IsAllowed);
        Assert.Equal("InheritedRule", childCheck.Value.Source);

        _ = await commands.SendAsync<
            SaveEntityAclRulesCommand,
            IReadOnlyList<EntityAclRuleResponse>
        >(
            new SaveEntityAclRulesCommand(
                "Ruth.Core.DefinedValue",
                childId,
                AuthorizationActions.Edit,
                [
                    new AclRuleInput(
                        AclRuleDecision.Deny,
                        SpecialRoleType.None,
                        UserAccountId: staff.Value.UserAccountId
                    ),
                ],
                onboard.Value.AdminUserId
            )
        );

        var childDenied = await queries.QueryAsync<
            VerifyEntityAuthorizationQuery,
            AclVerificationResponse
        >(
            new VerifyEntityAuthorizationQuery(
                "Ruth.Core.DefinedValue",
                childId,
                AuthorizationActions.Edit,
                staff.Value.UserAccountId,
                ParentEntityTypeKey: "Ruth.Core.DefinedType",
                ParentEntityId: parentId
            )
        );
        Assert.True(childDenied.IsSuccess);
        Assert.False(childDenied.Value.IsAllowed);
        Assert.Equal("LocalRule", childDenied.Value.Source);

        var audits = await queries.QueryAsync<
            GetAuthAuditLogsQuery,
            IReadOnlyList<AuthAuditLogResponse>
        >(new GetAuthAuditLogsQuery(MaxCount: 50));
        Assert.True(audits.IsSuccess);
        Assert.True(audits.Value.Count >= 2);

        var deleteAdmin = await commands.SendAsync<DeleteSecurityRoleCommand, bool>(
            new DeleteSecurityRoleCommand(SecurityRole.AdministratorsRoleId.Value)
        );
        Assert.True(deleteAdmin.IsFailure);
    }

    [Fact]
    public async Task RestApiKeys_AndOAuthClientsAndScopes_PersistAndEnforceUniqueness()
    {
        var connectionString = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = IdentityTestHostFactory.BuildServiceProvider(
            connectionString,
            autoMigrate: true
        );
        await IdentityTestHostFactory.StartBootstrapAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var onboard = await commands.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(IdentityTestHostFactory.CreateValidCommand());

        var createdKey = await commands.SendAsync<
            CreateRestApiKeyCommand,
            CreateRestApiKeyResponse
        >(new CreateRestApiKeyCommand("Sync Worker", "Nightly sync", onboard.Value.AdminUserId));
        Assert.True(createdKey.IsSuccess);
        Assert.StartsWith("ruth_", createdKey.Value.PlaintextApiKey, StringComparison.Ordinal);

        var revoked = await commands.SendAsync<RevokeRestApiKeyCommand, bool>(
            new RevokeRestApiKeyCommand(createdKey.Value.Key.Id)
        );
        Assert.True(revoked.IsSuccess);

        var client = await commands.SendAsync<CreateAuthClientCommand, AuthClientResponse>(
            new CreateAuthClientCommand(
                "Mobile App",
                "ruth-mobile",
                "secret-1",
                "ruth://callback",
                "ruth://logout",
                "openid profile",
                "email",
                AllowUserApiAccess: true
            )
        );
        Assert.True(client.IsSuccess);

        var dupClient = await commands.SendAsync<CreateAuthClientCommand, AuthClientResponse>(
            new CreateAuthClientCommand(
                "Dup",
                "ruth-mobile",
                ClientSecret: null,
                "ruth://callback",
                "ruth://logout",
                "openid",
                string.Empty,
                AllowUserApiAccess: false
            )
        );
        Assert.True(dupClient.IsFailure);

        var scopeCreated = await commands.SendAsync<CreateAuthScopeCommand, AuthScopeResponse>(
            new CreateAuthScopeCommand("crm.read", "Read CRM")
        );
        Assert.True(scopeCreated.IsSuccess);

        var scopes = await queries.QueryAsync<GetAuthScopesQuery, IReadOnlyList<AuthScopeResponse>>(
            new GetAuthScopesQuery()
        );
        Assert.True(scopes.IsSuccess);
        _ = Assert.Single(scopes.Value);
    }
}
