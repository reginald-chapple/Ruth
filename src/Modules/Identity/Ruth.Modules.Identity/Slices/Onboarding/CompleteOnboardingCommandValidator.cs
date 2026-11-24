using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Onboarding;

internal sealed class CompleteOnboardingCommandValidator
    : IRequestValidator<CompleteOnboardingCommand>
{
    public ValueTask<IReadOnlyList<DomainError>> ValidateAsync(
        CompleteOnboardingCommand request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var combined = Result.Combine(
            OnboardingPolicy.ValidateRequiredText(
                request.OrganizationName,
                OnboardingPolicy.OrganizationNameMaxLength,
                IdentityErrors.OrganizationNameRequired,
                IdentityErrors.OrganizationNameTooLong
            ),
            OnboardingPolicy.ValidateTimeZoneId(request.TimeZoneId),
            OnboardingPolicy.ValidateRequiredText(
                request.PrimaryCampusName,
                OnboardingPolicy.CampusNameMaxLength,
                IdentityErrors.PrimaryCampusNameRequired,
                IdentityErrors.PrimaryCampusNameTooLong
            ),
            OnboardingPolicy.ValidateOptionalText(
                request.PrimaryCampusShortCode,
                OnboardingPolicy.CampusShortCodeMaxLength,
                IdentityErrors.PrimaryCampusShortCodeTooLong
            ),
            OnboardingPolicy.ValidateRequiredText(
                request.AdminUserName,
                OnboardingPolicy.UserNameMaxLength,
                IdentityErrors.UserNameRequired,
                IdentityErrors.UserNameTooLong
            ),
            OnboardingPolicy.ValidateEmail(request.AdminEmail),
            OnboardingPolicy.ValidateRequiredText(
                request.AdminDisplayName,
                OnboardingPolicy.DisplayNameMaxLength,
                IdentityErrors.DisplayNameRequired,
                IdentityErrors.DisplayNameTooLong
            ),
            OnboardingPolicy.ValidatePasswordComplexity(request.AdminPassword)
        );

        return ValueTask.FromResult(combined.Errors);
    }
}
