using System.ComponentModel.DataAnnotations;
using Ruth.Host.Components.Pages;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class OnboardingTimeZoneValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OnboardingFormModel_EmptyOrWhitespaceTimeZoneId_FailsDataAnnotationsValidation(
        string timeZoneId
    )
    {
        var model = CreateValidModel();
        model.TimeZoneId = timeZoneId;

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true
        );

        Assert.False(isValid);
        Assert.Contains(results, static r =>
            r.MemberNames.Contains(nameof(OnboardingFormModel.TimeZoneId), StringComparer.Ordinal)
        );
    }

    [Fact]
    public void OnboardingFormModel_ValidTimeZoneId_PassesDataAnnotationsValidation()
    {
        var model = CreateValidModel();

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true
        );

        Assert.True(isValid);
        Assert.Empty(results);
    }

    private static OnboardingFormModel CreateValidModel()
    {
        return new OnboardingFormModel
        {
            OrganizationName = "Grace Fellowship",
            TimeZoneId = "UTC",
            PrimaryCampusName = "Main Campus",
            PrimaryCampusShortCode = "MAIN",
            AdminDisplayName = "Grace Admin",
            AdminUserName = "admin",
            AdminEmail = "admin@grace.org",
            AdminPassword = "StrongPass#2026!",
            ConfirmPassword = "StrongPass#2026!",
        };
    }
}
