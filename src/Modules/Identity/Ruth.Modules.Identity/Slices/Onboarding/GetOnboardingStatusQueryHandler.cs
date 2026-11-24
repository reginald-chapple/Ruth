using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Onboarding;

internal sealed class GetOnboardingStatusQueryHandler(IOnboardingStateReader stateReader)
    : IQueryHandler<GetOnboardingStatusQuery, OnboardingStatusResponse>
{
    private readonly IOnboardingStateReader _stateReader = stateReader
        ?? throw new ArgumentNullException(nameof(stateReader));

    public async Task<Result<OnboardingStatusResponse>> HandleAsync(
        GetOnboardingStatusQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var status = await _stateReader.GetStatusAsync(cancellationToken);
        return status;
    }
}
