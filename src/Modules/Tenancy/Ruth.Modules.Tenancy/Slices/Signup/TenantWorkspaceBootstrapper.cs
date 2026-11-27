using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Signup;

internal static class TenantWorkspaceBootstrapper
{
    public static async Task<Result<Guid>> BootstrapAsync(
        IServiceScopeFactory scopeFactory,
        Tenant tenant,
        SignupTaxExemptTenantCommand command,
        CancellationToken cancellationToken
    )
    {
        using (
            AmbientTenantContext.BeginScope(tenant.Id, tenant.Slug, TenantLifecycleStage.Sandbox)
        )
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var primaryBranch = string.IsNullOrWhiteSpace(command.PrimaryBranchName)
                ? "Headquarters"
                : command.PrimaryBranchName.Trim();

            var adminResult = await ProvisionInitialAdminAsync(
                scope.ServiceProvider,
                tenant,
                command,
                primaryBranch,
                cancellationToken
            );
            if (adminResult.IsFailure)
            {
                return adminResult;
            }

            await EnsurePrimaryBranchAsync(
                scope.ServiceProvider,
                tenant,
                primaryBranch,
                cancellationToken
            );

            var contributors = scope
                .ServiceProvider.GetServices<ITenantSeedContributor>()
                .OrderBy(static c => c.Order)
                .ToArray();
            foreach (var contributor in contributors)
            {
                await contributor.SeedTenantAsync(tenant.Id, cancellationToken);
            }

            var outboxDispatcher = scope.ServiceProvider.GetService<OutboxDispatcherService>();
            if (outboxDispatcher is not null)
            {
                _ = await outboxDispatcher.ProcessPendingOnceAsync(cancellationToken);
            }

            return adminResult.Value;
        }
    }

    private static async Task<Result<Guid>> ProvisionInitialAdminAsync(
        IServiceProvider services,
        Tenant tenant,
        SignupTaxExemptTenantCommand command,
        string primaryBranch,
        CancellationToken cancellationToken
    )
    {
        var onboardingHandler = services.GetService<
            ICommandHandler<CompleteOnboardingCommand, CompleteOnboardingResponse>
        >();
        if (onboardingHandler is null)
        {
            return Guid.Empty;
        }

        var dispatcher = services.GetRequiredService<ICommandDispatcher>();
        var onboardingResult = await dispatcher.SendAsync<
            CompleteOnboardingCommand,
            CompleteOnboardingResponse
        >(
            new CompleteOnboardingCommand(
                tenant.DisplayName,
                tenant.TimeZoneId,
                primaryBranch,
                "HQ",
                command.AdminUserName,
                command.AdminEmail,
                command.AdminDisplayName,
                command.AdminPassword
            ),
            cancellationToken
        );
        return onboardingResult.IsSuccess
            ? onboardingResult.Value.AdminUserId
            : Result.Failure<Guid>(onboardingResult.Errors);
    }

    private static async Task EnsurePrimaryBranchAsync(
        IServiceProvider services,
        Tenant tenant,
        string primaryBranch,
        CancellationToken cancellationToken
    )
    {
        var branchHandler = services.GetService<
            ICommandHandler<CreateBranchCommand, BranchResponse>
        >();
        if (branchHandler is null)
        {
            return;
        }

        var queryDispatcher = services.GetService<IQueryDispatcher>();
        if (queryDispatcher is not null)
        {
            var existing = await queryDispatcher.QueryAsync<
                GetBranchesQuery,
                IReadOnlyList<BranchResponse>
            >(new GetBranchesQuery(ActiveOnly: false), cancellationToken);
            if (existing.IsSuccess && existing.Value.Count > 0)
            {
                return;
            }
        }

        var dispatcher = services.GetRequiredService<ICommandDispatcher>();
        _ = await dispatcher.SendAsync<CreateBranchCommand, BranchResponse>(
            new CreateBranchCommand(
                Name: primaryBranch,
                ShortCode: "HQ",
                IsActive: true,
                TimeZoneId: tenant.TimeZoneId
            ),
            cancellationToken
        );
    }
}
