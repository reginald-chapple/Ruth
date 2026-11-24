using System.Text.Json.Serialization;
using Microsoft.FluentUI.AspNetCore.Components;
using Ruth.Host.Components;
using Ruth.Host.DependencyInjection;
using Ruth.Host.Endpoints;
using Ruth.Host.Health;
using Ruth.Host.Middleware;
using Ruth.Host.Observability;
using Ruth.Modules.Identity;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedKernel();
builder.Services.AddRuthPersistence(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddRuthJwtConfiguration();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(static options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);
builder.Services.Configure<RouteHandlerOptions>(static options => options.ThrowOnBadRequest = true);
builder.Services.AddOpenApi();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRuthRateLimiting();
builder.Services.AddOutputCache();
builder.Services.AddAntiforgery();

builder.AddRuthObservability();
builder.Services.AddRuthHealthChecks();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();

app.UseExceptionHandler();

app.UseWhen(
    static context =>
        HttpMethods.IsGet(context.Request.Method)
        && !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        && !context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
        && !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase),
    static branch =>
        _ = branch.UseStatusCodePagesWithReExecute(
            "/not-found",
            createScopeForStatusCodePages: true
        )
);

app.UseWhen(
    static context =>
        context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    static branch =>
        _ = branch.UseStatusCodePages(ProblemDetailsResponseWriter.WriteApiStatusCodeProblemAsync)
);

if (!app.Environment.IsDevelopment())
{
    _ = app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.UseMiddleware<OnboardingGateMiddleware>();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    _ = app.MapOpenApi().AllowAnonymous();
}

app.MapHealthEndpoints();
app.MapIdentityModuleEndpoints();

await app.RunAsync();
