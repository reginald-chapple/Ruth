using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal sealed class GetRegisteredPaymentGatewayProvidersQueryHandler(
    IPaymentGatewayRegistry registry
)
    : IQueryHandler<
        GetRegisteredPaymentGatewayProvidersQuery,
        IReadOnlyList<RegisteredPaymentGatewayProviderResponse>
    >
{
    public Task<Result<IReadOnlyList<RegisteredPaymentGatewayProviderResponse>>> HandleAsync(
        GetRegisteredPaymentGatewayProvidersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<RegisteredPaymentGatewayProviderResponse> providers =
        [
            .. registry
                .GetAll()
                .Select(static p => new RegisteredPaymentGatewayProviderResponse(
                    p.ProviderKey,
                    p.DisplayName
                )),
        ];
        return Task.FromResult(Result.Success(providers));
    }
}
