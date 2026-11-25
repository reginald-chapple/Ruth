using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Infrastructure.Templating;
using Ruth.Modules.Communication.Infrastructure.Templating.BuiltInTemplates;
using Ruth.Modules.Communication.Tests.Fixtures;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

public sealed class RazorTemplateRendererTests
{
    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        foreach (var descriptor in BuiltInRazorTemplatesCatalog.GetAll())
        {
            _ = services.AddSingleton(descriptor);
        }

        _ = services.AddRazorTemplate(
            new RazorTemplateDescriptor(
                "Tests.CustomGreeting",
                "Custom Greeting",
                "Test custom compiled Razor template",
                "Testing",
                typeof(SampleCustomRazorTemplate),
                [
                    new("DisplayName", "string", IsRequired: true, "Recipient display name"),
                    new("MemberId", "int", IsRequired: false, "Member ID"),
                    new("Amount", "decimal", IsRequired: false, "Amount"),
                ]
            )
        );
        _ = services.AddSingleton<IRazorTemplateRegistry, RazorTemplateRegistry>();
        _ = services.AddSingleton<IRazorTemplateRenderer, RazorHtmlTemplateRenderer>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Renders_StandardEmailTemplate_With_Xss_Encoding_And_Safe_ActionUrl_Guard()
    {
        await using var provider = CreateServices();
        var renderer = provider.GetRequiredService<IRazorTemplateRenderer>();

        var result = await renderer.RenderNamedTemplateAsync(
            BuiltInRazorTemplatesCatalog.StandardEmailKey,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["OrganizationName"] = "Grace Church",
                ["Subject"] = "Sunday Update",
                ["Preheader"] = "See what's happening this week",
                ["Heading"] = "Hello <script>bad()</script>",
                ["BodyHtml"] = "<p>Welcome <strong>Jordan</strong>!</p>",
                ["ActionLabel"] = "Give Now",
                ["ActionUrl"] = "https://grace.example/give",
                ["FooterText"] = "123 Main St",
            }
        );

        Assert.True(result.IsSuccess);
        Assert.Contains("Grace Church", result.Value, StringComparison.Ordinal);
        Assert.Contains(
            "Hello &lt;script&gt;bad()&lt;/script&gt;",
            result.Value,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "<p>Welcome <strong>Jordan</strong>!</p>",
            result.Value,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "href=\"https://grace.example/give\"",
            result.Value,
            StringComparison.Ordinal
        );

        // javascript: URLs must be stripped/not rendered as CTA links
        var unsafeUrl = await renderer.RenderComponentAsync<StandardEmailTemplate>(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["BodyText"] = "Plain text <img src=x>",
                ["ActionLabel"] = "Click Me",
                ["ActionUrl"] = "javascript:alert(1)",
            }
        );
        Assert.True(unsafeUrl.IsSuccess);
        Assert.Contains("Plain text &lt;img src=x&gt;", unsafeUrl.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:", unsafeUrl.Value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Renders_SimpleAlertTemplate_And_Custom_Registered_Razor_Component()
    {
        await using var provider = CreateServices();
        var renderer = provider.GetRequiredService<IRazorTemplateRenderer>();
        var registry = provider.GetRequiredService<IRazorTemplateRegistry>();

        Assert.Equal(3, registry.GetAll().Count);
        Assert.Null(registry.FindByKey("   "));

        var alert = await renderer.RenderNamedTemplateAsync(
            BuiltInRazorTemplatesCatalog.SimpleAlertKey,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Title"] = "Batch Closed",
                ["Message"] = "Batch #42 reconciled cleanly.",
                ["Tone"] = "success",
                ["Footnote"] = "Finance Module",
            }
        );
        Assert.True(alert.IsSuccess);
        Assert.Contains("#107c10", alert.Value, StringComparison.Ordinal);
        Assert.Contains("Batch #42 reconciled cleanly.", alert.Value, StringComparison.Ordinal);

        var custom = await renderer.RenderNamedTemplateAsync(
            "Tests.CustomGreeting",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["DisplayName"] = "Taylor",
                ["MemberId"] = 77,
                ["Amount"] = 45.25m,
            }
        );
        Assert.True(custom.IsSuccess);
        Assert.Contains("Hello Taylor", custom.Value, StringComparison.Ordinal);
        Assert.Contains("data-member-id=\"77\"", custom.Value, StringComparison.Ordinal);
        Assert.Contains("Total: 45.25", custom.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_Invalid_Component_Type_And_Unknown_Template_Key()
    {
        await using var provider = CreateServices();
        var renderer = provider.GetRequiredService<IRazorTemplateRenderer>();

        var notComponent = await renderer.RenderComponentAsync<string>();
        Assert.True(notComponent.IsFailure);
        Assert.Equal("Communication.InvalidRazorComponentType", notComponent.PrimaryError.Code);

        var missing = await renderer.RenderNamedTemplateAsync("Does.Not.Exist");
        Assert.True(missing.IsFailure);
        Assert.Equal("Communication.RazorTemplateNotFound", missing.PrimaryError.Code);
    }
}
