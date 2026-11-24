using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Components.Pages;

/// <summary>Form binding model for the first-run organization and administrator setup wizard.</summary>
public sealed class OnboardingFormModel
{
    /// <summary>Gets or sets the organization name.</summary>
    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(200, ErrorMessage = "Organization name cannot exceed 200 characters.")]
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization time zone identifier.</summary>
    [Required(ErrorMessage = "Time zone is required.")]
    [StringLength(50, ErrorMessage = "Time zone identifier cannot exceed 50 characters.")]
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary campus name.</summary>
    [Required(ErrorMessage = "Primary campus name is required.")]
    [StringLength(100, ErrorMessage = "Primary campus name cannot exceed 100 characters.")]
    public string PrimaryCampusName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional primary campus short code.</summary>
    [StringLength(50, ErrorMessage = "Primary campus short code cannot exceed 50 characters.")]
    public string? PrimaryCampusShortCode { get; set; }

    /// <summary>Gets or sets the administrator display name.</summary>
    [Required(ErrorMessage = "Administrator display name is required.")]
    [StringLength(150, ErrorMessage = "Administrator display name cannot exceed 150 characters.")]
    public string AdminDisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the administrator login username.</summary>
    [Required(ErrorMessage = "Administrator username is required.")]
    [StringLength(100, ErrorMessage = "Administrator username cannot exceed 100 characters.")]
    public string AdminUserName { get; set; } = string.Empty;

    /// <summary>Gets or sets the administrator email address.</summary>
    [Required(ErrorMessage = "Administrator email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(256, ErrorMessage = "Administrator email cannot exceed 256 characters.")]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the administrator password.</summary>
    [Required(ErrorMessage = "Administrator password is required.")]
    [StringLength(
        128,
        MinimumLength = 12,
        ErrorMessage = "Password must be between 12 and 128 characters."
    )]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the password confirmation value.</summary>
    [Required(ErrorMessage = "Please confirm the administrator password.")]
    [Compare(nameof(AdminPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
