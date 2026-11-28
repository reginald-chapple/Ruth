namespace Ruth.Modules.Tenancy.Infrastructure;

/// <summary>Configuration options governing SaaS host routing, sandbox subdomains, and mockable Stripe Connect.</summary>
public sealed class TenancyOptions
{
    /// <summary>Gets or sets the base domain appended to tenant slugs during Sandbox provisioning.</summary>
    public string SandboxBaseDomain { get; set; } = "sandbox.localhost";

    /// <summary>Gets or sets the dedicated internal Platform Operator hostname.</summary>
    public string PlatformOperatorHost { get; set; } = "ops.localhost";

    /// <summary>Gets or sets the hostnames serving the self-service tax-exempt signup portal.</summary>
    public IList<string> PlatformSignupHosts { get; } = ["localhost", "signup.localhost"];

    /// <summary>Gets or sets whether Stripe Connect runs in deterministic zero-config mock mode.</summary>
    public bool StripeMockMode { get; set; } = true;

    /// <summary>Default local development HMAC-SHA256 signing secret for Stripe Connect webhooks.</summary>
    public const string DefaultDevelopmentStripeConnectWebhookSecret =
        "whsec_dev_stripe_connect_signing_secret_change_in_prod";

    /// <summary>Gets or sets the HMAC-SHA256 signing secret required on inbound Stripe Connect webhooks.</summary>
    public string StripeConnectWebhookSecret { get; set; } =
        DefaultDevelopmentStripeConnectWebhookSecret;

    /// <summary>Gets or sets the maximum allowed timestamp skew in seconds on inbound Stripe Connect webhooks.</summary>
    public int WebhookReplayToleranceSeconds { get; set; } = 300;

    /// <summary>Gets or sets whether custom domain DNS TXT verification runs in deterministic mock mode.</summary>
    public bool DnsMockMode { get; set; } = true;

    /// <summary>Gets or sets an optional initial Platform Operator email seeded when Bootstrap:AutoMigrateAndSeed is enabled.</summary>
    public string? SeedOperatorEmail { get; set; }

    /// <summary>Gets or sets an optional initial Platform Operator password seeded on startup.</summary>
    public string? SeedOperatorPassword { get; set; }

    /// <summary>Gets or sets the display name for the seeded Platform Operator account.</summary>
    public string SeedOperatorDisplayName { get; set; } = "Platform Compliance Operator";

    /// <summary>
    /// Gets or sets an optional fixed workspace tenant identifier bound to loopback hosts strictly
    /// for single-workspace test harnesses; remains null in multi-tenant SaaS deployments.
    /// </summary>
    public Guid? DedicatedWorkspaceTenantId { get; set; }
}
