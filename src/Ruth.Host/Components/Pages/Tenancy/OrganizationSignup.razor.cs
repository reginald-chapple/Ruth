using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Pages.Tenancy;

/// <summary>Self-service Sandbox workspace signup page for IRS-recognized tax-exempt organizations.</summary>
public sealed partial class OrganizationSignup : ComponentBase
{
    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private ITenantContext TenantContext { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    internal OrganizationSignupFormModel Model { get; } = new();

    internal IReadOnlyList<TimeZoneOption> TimeZones { get; private set; } = [];

    internal SignupTaxExemptTenantResponse? ProvisionedWorkspace { get; private set; }

    internal bool IsSubmitting { get; private set; }

    internal string? ErrorMessage { get; private set; }

    internal bool IsAllowedHost =>
        !TenantContext.HasTenant && !TenantContext.IsPlatformOperatorHost;

    internal string SandboxLoginUrl
    {
        get
        {
            if (ProvisionedWorkspace is null)
            {
                return "/";
            }

            var currentUri = new Uri(Navigation.Uri);
            var portSuffix = currentUri.IsDefaultPort
                ? string.Empty
                : string.Create(CultureInfo.InvariantCulture, $":{currentUri.Port}");
            return $"{currentUri.Scheme}://{ProvisionedWorkspace.SandboxHostname}{portSuffix}/login";
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        TimeZones =
        [
            .. TimeZoneInfo
                .GetSystemTimeZones()
                .Select(static tz => new TimeZoneOption(tz.Id, tz.DisplayName)),
        ];
    }

    internal async Task SubmitAsync()
    {
        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            var result = await CommandDispatcher.SendAsync<
                SignupTaxExemptTenantCommand,
                SignupTaxExemptTenantResponse
            >(
                new SignupTaxExemptTenantCommand(
                    Model.LegalName.Trim(),
                    Model.DisplayName.Trim(),
                    Model.Slug.Trim().ToLowerInvariant(),
                    Model.Ein.Trim(),
                    Model.IrsSubsectionCode.Trim(),
                    Model.StateCode.Trim().ToUpperInvariant(),
                    Model.TimeZoneId,
                    Model.PrimaryBranchName.Trim(),
                    Model.AdminUserName.Trim(),
                    Model.AdminEmail.Trim(),
                    Model.AdminDisplayName.Trim(),
                    Model.AdminPassword
                )
            );

            if (result.IsFailure)
            {
                ErrorMessage = string.Join(' ', result.Errors.Select(static e => e.Description));
                return;
            }

            ProvisionedWorkspace = result.Value;
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
