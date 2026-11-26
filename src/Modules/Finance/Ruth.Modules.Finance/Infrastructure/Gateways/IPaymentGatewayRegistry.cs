using Ruth.Modules.Finance.Contracts.Gateways;

namespace Ruth.Modules.Finance.Infrastructure.Gateways;

internal interface IPaymentGatewayRegistry
{
    bool TryGet(string providerKey, out IPaymentGateway provider);

    IReadOnlyList<IPaymentGateway> GetAll();
}
