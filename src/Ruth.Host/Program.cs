using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR;
using Microsoft.FluentUI.AspNetCore.Components;
using Ruth.Host.Components;
using Ruth.Host.Components.Cms;
using Ruth.Host.DependencyInjection;
using Ruth.Host.Health;
using Ruth.Host.Middleware;
using Ruth.Host.Observability;
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
using Ruth.Persistence.DependencyInjection;
using Ruth.Providers.Checkr;
using Ruth.Providers.CommunicationTransports;
using Ruth.Providers.PaymentGateways;
using Ruth.Providers.RustFsStorage;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(static options => options.AddServerHeader = false);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IPrincipalTenantValidator, HttpContextPrincipalTenantValidator>();
builder.Services.AddScoped<TenantWorkspaceBrandingState>();
builder.Services.AddScoped<TenantCircuitState>();
builder.Services.AddScoped<CircuitHandler, TenantCircuitHandler>();
builder.Services.AddSingleton<TenantSignalRHubFilter>();
builder.Services.AddSignalR(static options => options.AddFilter<TenantSignalRHubFilter>());

builder.Services.AddSharedKernel();
builder.Services.AddRuthPersistence(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddRustFsStorageProvider(builder.Configuration);
builder.Services.AddCoreModule(builder.Configuration);
builder.Services.AddCheckrBackgroundCheckProvider(builder.Configuration);
builder.Services.AddCrmModule(builder.Configuration);
builder.Services.AddGroupsModule(builder.Configuration);
builder.Services.AddCheckInModule(builder.Configuration);
builder.Services.AddCommunicationModule(builder.Configuration);
builder.Services.AddCommunicationTransportProviders(builder.Configuration);
builder.Services.AddFinanceModule(builder.Configuration);
builder.Services.AddPaymentGatewayProviders(builder.Configuration);
builder.Services.AddEventsModule(builder.Configuration);
builder.Services.AddCommerceModule(builder.Configuration);
builder.Services.AddEngagementModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddCmsModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddLmsModule(builder.Configuration);
builder.Services.AddSearchModule(builder.Configuration);
builder.Services.AddTenancyModule(builder.Configuration);
builder.Services.AddSingleton<RuthBlockComponentRegistry>();
builder.Services.AddRuthJwtConfiguration();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(static options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);
builder.Services.Configure<RouteHandlerOptions>(static options => options.ThrowOnBadRequest = true);
builder.Services.AddOpenApi();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRuthForwardedHeaders();
builder.Services.AddRuthRateLimiting();
builder.Services.AddOutputCache();
builder.Services.AddAntiforgery(static options =>
{
    options.Cookie.Name = StealthResponseHeadersMiddleware.PlainCsrfCookieName;
    options.Cookie.Domain = null;
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.AddRuthObservability();
builder.Services.AddRuthHealthChecks();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();

app.UseMiddleware<StealthResponseHeadersMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<ProxyAndHostGuardMiddleware>();
app.UseForwardedHeaders();
app.UseRuthStatusCodeBranches();

if (!app.Environment.IsDevelopment())
{
    _ = app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseMiddleware<OnboardingGateMiddleware>();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (
    !app.Environment.IsProduction()
    && (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
)
{
    _ = app.MapOpenApi().AllowAnonymous();
}

app.MapRuthAllEndpoints();

await app.RunAsync();
