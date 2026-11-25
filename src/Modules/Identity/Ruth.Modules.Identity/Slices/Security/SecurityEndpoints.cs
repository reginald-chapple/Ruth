using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.Modules.Identity.Slices.Onboarding;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Slices.Security;

internal static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/security")
            .WithTags("Security Roles & API Keys")
            .RequireAuthorization(IdentityAuthorizationPolicies.AdministratorsOnly);

        MapRoleEndpoints(group);
        MapApiKeyEndpoints(group);
        return endpoints;
    }

    private static void MapRoleEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/roles", GetRolesAsync)
            .WithName("GetSecurityRoles")
            .WithSummary("List security roles")
            .Produces<IReadOnlyList<SecurityRoleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/roles", CreateRoleAsync)
            .WithName("CreateSecurityRole")
            .WithSummary("Create a custom security role")
            .Produces<SecurityRoleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/roles/{roleId:guid}", UpdateRoleAsync)
            .WithName("UpdateSecurityRole")
            .WithSummary("Update a security role")
            .Produces<SecurityRoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/roles/{roleId:guid}", DeleteRoleAsync)
            .WithName("DeleteSecurityRole")
            .WithSummary("Delete a non-system security role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/roles/{roleId:guid}/members/{userAccountId:guid}", AssignRoleAsync)
            .WithName("AssignUserToSecurityRole")
            .WithSummary("Assign a user account to a security role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapDelete("/roles/{roleId:guid}/members/{userAccountId:guid}", RemoveRoleAsync)
            .WithName("RemoveUserFromSecurityRole")
            .WithSummary("Remove a user account from a security role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapApiKeyEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/api-keys", GetApiKeysAsync)
            .WithName("GetRestApiKeys")
            .WithSummary("List REST API keys")
            .Produces<IReadOnlyList<RestApiKeyResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/api-keys", CreateApiKeyAsync)
            .WithName("CreateRestApiKey")
            .WithSummary("Generate a new hashed REST API key (plaintext returned once)")
            .Produces<CreateRestApiKeyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapDelete("/api-keys/{apiKeyId:guid}", RevokeApiKeyAsync)
            .WithName("RevokeRestApiKey")
            .WithSummary("Revoke an active REST API key")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SecurityRoleResponse>>, ProblemHttpResult>
    > GetRolesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSecurityRolesQuery,
            IReadOnlyList<SecurityRoleResponse>
        >(new GetSecurityRolesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SecurityRoleResponse>, ProblemHttpResult>
    > CreateRoleAsync(
        CreateSecurityRoleRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateSecurityRoleCommand, SecurityRoleResponse>(
            new CreateSecurityRoleCommand(request.Name, request.Description),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/security/roles/{result.Value.Id:D}", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<SecurityRoleResponse>, ProblemHttpResult>> UpdateRoleAsync(
        Guid roleId,
        UpdateSecurityRoleRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateSecurityRoleCommand, SecurityRoleResponse>(
            new UpdateSecurityRoleCommand(
                roleId,
                request.Name,
                request.Description,
                request.IsActive
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteRoleAsync(
        Guid roleId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteSecurityRoleCommand, bool>(
            new DeleteSecurityRoleCommand(roleId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AssignRoleAsync(
        Guid roleId,
        Guid userAccountId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<AssignUserRoleCommand, bool>(
            new AssignUserRoleCommand(userAccountId, roleId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveRoleAsync(
        Guid roleId,
        Guid userAccountId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RemoveUserRoleCommand, bool>(
            new RemoveUserRoleCommand(userAccountId, roleId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RestApiKeyResponse>>, ProblemHttpResult>
    > GetApiKeysAsync(
        Guid? userAccountId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetRestApiKeysQuery,
            IReadOnlyList<RestApiKeyResponse>
        >(new GetRestApiKeysQuery(userAccountId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CreateRestApiKeyResponse>, ProblemHttpResult>
    > CreateApiKeyAsync(
        CreateRestApiKeyRequest request,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var ownerId =
            request.UserAccountId != Guid.Empty
                ? request.UserAccountId
                : (httpContext.User.GetUserAccountId() ?? Guid.Empty);
        var result = await dispatcher.SendAsync<CreateRestApiKeyCommand, CreateRestApiKeyResponse>(
            new CreateRestApiKeyCommand(
                request.Name,
                request.Description,
                ownerId,
                request.ExpiresOnUtc
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                new Uri($"/api/security/api-keys/{result.Value.Key.Id:D}", UriKind.Relative),
                result.Value
            )
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeApiKeyAsync(
        Guid apiKeyId,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<RevokeRestApiKeyCommand, bool>(
            new RevokeRestApiKeyCommand(apiKeyId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : OnboardingProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
