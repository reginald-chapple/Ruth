using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Onboarding;

/// <summary>Query to retrieve the current first-run onboarding status.</summary>
public sealed record GetOnboardingStatusQuery : IQuery<OnboardingStatusResponse>;
