using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Slices.Security;

internal static class AclManagementEndpoints
{
    public static IEndpointRouteBuilder MapAclManagementEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/security")
            .WithTags("Entity ACL & Audit")
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);

        _ = group
            .MapGet("/acl", GetAclRulesAsync)
            .WithName("GetEntityAclRules")
            .WithSummary("Get ordered ACL rules for a secured entity")
            .Produces<IReadOnlyList<EntityAclRuleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPut("/acl", SaveAclRulesAsync)
            .WithName("SaveEntityAclRules")
            .WithSummary("Atomically replace ordered ACL rules for an entity action")
            .Produces<IReadOnlyList<EntityAclRuleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapGet("/verify", VerifyAuthorizationAsync)
            .WithName("VerifyEntityAuthorization")
            .WithSummary("Verify and explain hierarchical ACL evaluation for a user and entity")
            .Produces<AclVerificationResponse>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/audit-logs", GetAuditLogsAsync)
            .WithName("GetAuthAuditLogs")
            .WithSummary("List recent ACL rule change audit entries")
            .Produces<IReadOnlyList<AuthAuditLogResponse>>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EntityAclRuleResponse>>, ProblemHttpResult>
    > GetAclRulesAsync(
        string entityTypeKey,
        Guid? entityId,
        string? action,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetEntityAclRulesQuery,
            IReadOnlyList<EntityAclRuleResponse>
        >(
            new GetEntityAclRulesQuery(entityTypeKey, entityId ?? Guid.Empty, action),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EntityAclRuleResponse>>, ProblemHttpResult>
    > SaveAclRulesAsync(
        SaveEntityAclRulesRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SaveEntityAclRulesCommand,
            IReadOnlyList<EntityAclRuleResponse>
        >(
            new SaveEntityAclRulesCommand(
                request.EntityTypeKey,
                request.EntityId,
                request.Action,
                request.Rules,
                httpContext.User.GetUserAccountId()
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<AclVerificationResponse>, ProblemHttpResult>
    > VerifyAuthorizationAsync(
        string entityTypeKey,
        Guid? entityId,
        string action,
        Guid? userAccountId,
        string? parentEntityTypeKey,
        Guid? parentEntityId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            VerifyEntityAuthorizationQuery,
            AclVerificationResponse
        >(
            new VerifyEntityAuthorizationQuery(
                entityTypeKey,
                entityId ?? Guid.Empty,
                action,
                userAccountId,
                parentEntityTypeKey,
                parentEntityId
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<AuthAuditLogResponse>>, ProblemHttpResult>
    > GetAuditLogsAsync(
        string? entityTypeKey,
        Guid? entityId,
        int? maxCount,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetAuthAuditLogsQuery,
            IReadOnlyList<AuthAuditLogResponse>
        >(new GetAuthAuditLogsQuery(entityTypeKey, entityId, maxCount ?? 100), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
