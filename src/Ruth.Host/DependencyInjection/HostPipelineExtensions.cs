using Ruth.Host.Endpoints;
using Ruth.Host.Middleware;
using Ruth.Modules.CheckIn;
using Ruth.Modules.Cms;
using Ruth.Modules.Commerce;
using Ruth.Modules.Communication;
using Ruth.Modules.Core;
using Ruth.Modules.Crm;
using Ruth.Modules.Engagement;
using Ruth.Modules.Events;
using Ruth.Modules.Finance;
using Ruth.Modules.Groups;
using Ruth.Modules.Identity;
using Ruth.Modules.Lms;
using Ruth.Modules.Reporting;
using Ruth.Modules.Search;
using Ruth.Modules.Tenancy;
using Ruth.Modules.Workflow;

namespace Ruth.Host.DependencyInjection;

internal static class HostPipelineExtensions
{
    public static void UseRuthStatusCodeBranches(this WebApplication app)
    {
        _ = app.UseWhen(
            static context =>
                HttpMethods.IsGet(context.Request.Method)
                && !context.Items.ContainsKey(
                    TenantResolutionMiddleware.StealthPlainNotFoundItemKey
                )
                && !context.Request.Path.StartsWithSegments(
                    "/api",
                    StringComparison.OrdinalIgnoreCase
                )
                && !context.Request.Path.StartsWithSegments(
                    "/hubs",
                    StringComparison.OrdinalIgnoreCase
                )
                && !context.Request.Path.StartsWithSegments(
                    "/openapi",
                    StringComparison.OrdinalIgnoreCase
                )
                && !context.Request.Path.StartsWithSegments(
                    "/health",
                    StringComparison.OrdinalIgnoreCase
                ),
            static branch =>
                _ = branch.UseStatusCodePagesWithReExecute(
                    "/not-found",
                    createScopeForStatusCodePages: true
                )
        );

        _ = app.UseWhen(
            static context =>
                context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
                && !context.Items.ContainsKey(
                    TenantResolutionMiddleware.StealthPlainNotFoundItemKey
                ),
            static branch =>
                _ = branch.UseStatusCodePages(
                    ProblemDetailsResponseWriter.WriteApiStatusCodeProblemAsync
                )
        );
    }

    public static void MapRuthAllEndpoints(this WebApplication app)
    {
        _ = app.MapHealthEndpoints();
        _ = app.MapTenancyModuleEndpoints();
        _ = app.MapIdentityModuleEndpoints();
        _ = app.MapCoreModuleEndpoints();
        _ = app.MapCrmModuleEndpoints();
        _ = app.MapGroupsModuleEndpoints();
        _ = app.MapCheckInModuleEndpoints();
        _ = app.MapCommunicationModuleEndpoints();
        _ = app.MapFinanceModuleEndpoints();
        _ = app.MapEventsModuleEndpoints();
        _ = app.MapCommerceModuleEndpoints();
        _ = app.MapEngagementModuleEndpoints();
        _ = app.MapWorkflowModuleEndpoints();
        _ = app.MapCmsModuleEndpoints();
        _ = app.MapReportingModuleEndpoints();
        _ = app.MapLmsModuleEndpoints();
        _ = app.MapSearchModuleEndpoints();
    }
}
