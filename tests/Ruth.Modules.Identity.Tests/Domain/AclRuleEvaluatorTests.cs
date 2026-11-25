using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.Modules.Identity.Tests.Domain;

public sealed class AclRuleEvaluatorTests
{
    private const string ItemType = "Ruth.Test.Item";
    private const string ParentType = "Ruth.Test.Parent";
    private const string PreParentType = "Ruth.Test.PreParent";

    [Fact]
    public void Evaluate_LocalRules_RespectsPriorityOrderFirstMatchWins()
    {
        var roleId = SecurityRoleId.Create();
        var userId = UserAccountId.Create();
        var itemId = Guid.CreateVersion7();

        var denyUser = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Deny,
                SpecialRoleType.None,
                userAccountId: userId
            )
            .Value;
        var allowRole = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.Edit,
                order: 1,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                securityRoleId: roleId
            )
            .Value;

        var snapshot = AclRuleSnapshot.Create([allowRole, denyUser], [roleId.Value]);
        var item = new SecuredEntityRef(ItemType, itemId, GlobalDefaultSecured.Instance);
        var subject = new SecuritySubject(userId.Value, [roleId.Value]);

        var trace = AclRuleEvaluator.Evaluate(snapshot, item, AuthorizationActions.Edit, subject);

        Assert.False(trace.IsAllowed);
        Assert.Equal("LocalRule", trace.Source);
        Assert.Equal(denyUser.Id.Value, trace.MatchedRuleId);
        Assert.Equal(0, trace.MatchedOrder);
        Assert.False(
            AclRuleEvaluator.IsAuthorized(snapshot, item, AuthorizationActions.Edit, subject)
        );
    }

    [Fact]
    public void Evaluate_SpecialRoles_MatchAllUsersAuthenticatedAndUnauthenticatedCorrectly()
    {
        var itemId = Guid.CreateVersion7();
        var item = new SecuredEntityRef(ItemType, itemId, GlobalRestrictedDefaultSecured.Instance);

        var allowUnauth = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.View,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.AllUnauthenticatedUsers
            )
            .Value;
        var denyAuth = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.View,
                order: 1,
                AclRuleDecision.Deny,
                SpecialRoleType.AllAuthenticatedUsers
            )
            .Value;
        var allowAllEdit = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.AllUsers
            )
            .Value;

        var snapshot = AclRuleSnapshot.Create([allowUnauth, denyAuth, allowAllEdit], []);
        var authed = new SecuritySubject(Guid.CreateVersion7(), []);

        Assert.True(
            AclRuleEvaluator.IsAuthorized(
                snapshot,
                item,
                AuthorizationActions.View,
                SecuritySubject.Anonymous
            )
        );
        Assert.False(
            AclRuleEvaluator.IsAuthorized(snapshot, item, AuthorizationActions.View, authed)
        );
        Assert.True(
            AclRuleEvaluator.IsAuthorized(
                snapshot,
                item,
                AuthorizationActions.Edit,
                SecuritySubject.Anonymous
            )
        );
        Assert.True(
            AclRuleEvaluator.IsAuthorized(snapshot, item, AuthorizationActions.Edit, authed)
        );
    }

    [Fact]
    public void Evaluate_InactiveSecurityRole_IsIgnoredDuringEvaluation()
    {
        var activeRole = SecurityRoleId.Create();
        var inactiveRole = SecurityRoleId.Create();
        var itemId = Guid.CreateVersion7();

        var ruleInactive = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                securityRoleId: inactiveRole
            )
            .Value;
        var ruleActive = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.Administrate,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                securityRoleId: activeRole
            )
            .Value;

        var snapshot = AclRuleSnapshot.Create([ruleInactive, ruleActive], [activeRole.Value]);
        var item = new SecuredEntityRef(ItemType, itemId, GlobalDefaultSecured.Instance);
        var subject = new SecuritySubject(
            Guid.CreateVersion7(),
            [inactiveRole.Value, activeRole.Value]
        );

        Assert.False(
            AclRuleEvaluator.IsAuthorized(snapshot, item, AuthorizationActions.Edit, subject)
        );
        Assert.True(
            AclRuleEvaluator.IsAuthorized(
                snapshot,
                item,
                AuthorizationActions.Administrate,
                subject
            )
        );
    }

    [Fact]
    public void Evaluate_ParentAuthorityPre_IsCheckedLocallyWithoutRecursingIntoItsParent()
    {
        var roleId = SecurityRoleId.Create();
        var preGrandParentId = Guid.CreateVersion7();
        var preParentId = Guid.CreateVersion7();
        var mainParentId = Guid.CreateVersion7();
        var childId = Guid.CreateVersion7();

        var preGrandParent = new SecuredEntityRef(
            ParentType,
            preGrandParentId,
            GlobalDefaultSecured.Instance
        );
        var preParent = new SecuredEntityRef(PreParentType, preParentId, preGrandParent);
        var mainParent = new SecuredEntityRef(
            ParentType,
            mainParentId,
            GlobalDefaultSecured.Instance
        );
        var child = new SecuredEntityRef(ItemType, childId, mainParent, preParent);

        var ruleOnPreGrandParent = EntityAclRule
            .Create(
                ParentType,
                preGrandParentId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Deny,
                SpecialRoleType.AllUsers
            )
            .Value;
        var ruleOnMainParent = EntityAclRule
            .Create(
                ParentType,
                mainParentId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                securityRoleId: roleId
            )
            .Value;

        var snapshotWithoutPreLocal = AclRuleSnapshot.Create(
            [ruleOnPreGrandParent, ruleOnMainParent],
            [roleId.Value]
        );
        var subject = new SecuritySubject(Guid.CreateVersion7(), [roleId.Value]);

        // PreParent has no local rule, so its parent (preGrandParent) MUST NOT be consulted; falls through to mainParent!
        var trace = AclRuleEvaluator.Evaluate(
            snapshotWithoutPreLocal,
            child,
            AuthorizationActions.Edit,
            subject
        );
        Assert.True(trace.IsAllowed);
        Assert.Equal("InheritedRule", trace.Source);
        Assert.Equal(mainParentId, trace.MatchedEntityId);

        // When PreParent HAS a local rule, it wins before mainParent!
        var ruleOnPreLocal = EntityAclRule
            .Create(
                PreParentType,
                preParentId,
                AuthorizationActions.Edit,
                order: 0,
                AclRuleDecision.Deny,
                SpecialRoleType.AllUsers
            )
            .Value;
        var snapshotWithPreLocal = AclRuleSnapshot.Create(
            [ruleOnPreLocal, ruleOnMainParent],
            [roleId.Value]
        );
        var preTrace = AclRuleEvaluator.Evaluate(
            snapshotWithPreLocal,
            child,
            AuthorizationActions.Edit,
            subject
        );
        Assert.False(preTrace.IsAllowed);
        Assert.Equal("ParentAuthorityPre", preTrace.Source);
        Assert.Equal(preParentId, preTrace.MatchedEntityId);
    }

    [Fact]
    public void Evaluate_CyclesAndDeepChains_TerminateSafelyAndFallbackToDefault()
    {
        var nodeA = new CyclicSecured(ItemType, Guid.CreateVersion7());
        var nodeB = new CyclicSecured(ItemType, Guid.CreateVersion7());
        nodeA.Parent = nodeB;
        nodeB.Parent = nodeA;

        var cycleView = AclRuleEvaluator.Evaluate(
            AclRuleSnapshot.Empty,
            nodeA,
            AuthorizationActions.View,
            SecuritySubject.Anonymous
        );
        Assert.True(cycleView.IsAllowed);
        Assert.Equal("Default", cycleView.Source);

        var cycleEdit = AclRuleEvaluator.Evaluate(
            AclRuleSnapshot.Empty,
            nodeA,
            AuthorizationActions.Edit,
            SecuritySubject.Anonymous
        );
        Assert.False(cycleEdit.IsAllowed);
        Assert.Equal("Default", cycleEdit.Source);

        // Chain exceeding MaxHierarchyDepth (105 nodes)
        ISecured current = GlobalDefaultSecured.Instance;
        for (var i = 0; i < AclRuleEvaluator.MaxHierarchyDepth + 5; i++)
        {
            current = new SecuredEntityRef(ItemType, Guid.CreateVersion7(), current);
        }

        var deepTrace = AclRuleEvaluator.Evaluate(
            AclRuleSnapshot.Empty,
            current,
            AuthorizationActions.Tag,
            SecuritySubject.Anonymous
        );
        Assert.True(deepTrace.IsAllowed);
        Assert.Equal("Default", deepTrace.Source);
    }

    [Fact]
    public void IsPrivate_DetectsExactTwoRulePrivatePattern()
    {
        var ownerId = UserAccountId.Create();
        var otherUserId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var item = new SecuredEntityRef(ItemType, itemId, GlobalDefaultSecured.Instance);

        var allowOwner = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.View,
                order: 0,
                AclRuleDecision.Allow,
                SpecialRoleType.None,
                userAccountId: ownerId
            )
            .Value;
        var denyAll = EntityAclRule
            .Create(
                ItemType,
                itemId,
                AuthorizationActions.View,
                order: 1,
                AclRuleDecision.Deny,
                SpecialRoleType.AllUsers
            )
            .Value;

        var snapshot = AclRuleSnapshot.Create([allowOwner, denyAll], []);

        Assert.True(
            AclRuleEvaluator.IsPrivate(snapshot, item, AuthorizationActions.View, ownerId.Value)
        );
        Assert.False(
            AclRuleEvaluator.IsPrivate(snapshot, item, AuthorizationActions.View, otherUserId)
        );
        Assert.False(
            AclRuleEvaluator.IsPrivate(snapshot, item, AuthorizationActions.View, Guid.Empty)
        );
        Assert.False(
            AclRuleEvaluator.IsPrivate(
                AclRuleSnapshot.Empty,
                item,
                AuthorizationActions.View,
                ownerId.Value
            )
        );
    }

    [Fact]
    public void DomainInvariants_EnforceAclRuleTargetLockoutAndTokenRules()
    {
        // ACL rule must have exactly one target
        Assert.True(
            EntityAclRule
                .Create(
                    ItemType,
                    Guid.Empty,
                    "View",
                    0,
                    AclRuleDecision.Allow,
                    SpecialRoleType.None
                )
                .IsFailure
        );
        Assert.True(
            EntityAclRule
                .Create(
                    ItemType,
                    Guid.Empty,
                    "View",
                    0,
                    AclRuleDecision.Allow,
                    SpecialRoleType.AllUsers,
                    securityRoleId: SecurityRoleId.Create()
                )
                .IsFailure
        );

        // UserAccount lockout threshold and unlock on password change
        var now = DateTimeOffset.UtcNow;
        var account = UserAccount
            .Create("jdoe", "jdoe@example.org", "Jane Doe", "hash1", isConfirmed: true, now)
            .Value;
        for (var i = 0; i < AuthenticationPolicy.MaxFailedPasswordAttempts; i++)
        {
            account.RecordFailedLogin(now.AddSeconds(i));
        }

        Assert.True(account.IsCurrentlyLockedOut(now.AddMinutes(1)));
        Assert.False(account.IsCurrentlyLockedOut(now.AddMinutes(30)));
        Assert.True(account.ChangePassword("hash2", now.AddMinutes(2)).IsSuccess);
        Assert.False(account.IsLockedOut);

        // System role cannot be deactivated
        var sysAdmin = SecurityRole.CreateSystemAdministrators();
        Assert.True(sysAdmin.Update("Administrators", "Desc", isActive: false).IsFailure);

        // RestApiKey expiration & revocation
        var key = RestApiKey
            .Create("Sync", description: null, "ruth_1234", "hash", account.Id, now.AddHours(1))
            .Value;
        Assert.True(key.IsValidAt(now));
        Assert.False(key.IsValidAt(now.AddHours(2)));
        key.Revoke(now);
        Assert.False(key.IsValidAt(now));
    }

    private sealed class CyclicSecured(string typeKey, Guid id) : ISecured
    {
        public ISecured? Parent { get; set; }
        public string EntityTypeKey => typeKey;
        public Guid EntityId => id;
        public ISecured? ParentAuthority => Parent;

        public bool IsAllowedByDefault(string action)
        {
            return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
        }
    }
}
