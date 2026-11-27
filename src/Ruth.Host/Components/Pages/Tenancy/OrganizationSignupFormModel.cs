using System.ComponentModel.DataAnnotations;

namespace Ruth.Host.Components.Pages.Tenancy;

internal sealed class OrganizationSignupFormModel
{
    [Required]
    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        @"^\d{2}-?\d{7}$",
        ErrorMessage = "Enter a valid 9-digit IRS EIN (XX-XXXXXXX)."
    )]
    public string Ein { get; set; } = string.Empty;

    [Required]
    public string IrsSubsectionCode { get; set; } = "501(c)(3)";

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string StateCode { get; set; } = "DE";

    [Required]
    public string TimeZoneId { get; set; } = "UTC";

    [Required]
    [MaxLength(160)]
    public string PrimaryBranchName { get; set; } = "Main Center";

    [Required]
    [MaxLength(160)]
    public string AdminDisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AdminUserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    public string AdminPassword { get; set; } = string.Empty;
}
