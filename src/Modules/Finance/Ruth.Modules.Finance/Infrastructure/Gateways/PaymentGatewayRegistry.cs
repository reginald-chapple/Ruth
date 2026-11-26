using System.Collections.Frozen;
using Ruth.Modules.Finance.Contracts.Gateways;

namespace Ruth.Modules.Finance.Infrastructure.Gateways;

internal sealed class PaymentGatewayRegistry : IPaymentGatewayRegistry
{
    private readonly FrozenDictionary<string, IPaymentGateway> _byKey;
    private readonly IPaymentGateway[] _ordered;

    public PaymentGatewayRegistry(IEnumerable<IPaymentGateway> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var dict = new Dictionary<string, IPaymentGateway>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            dict[provider.ProviderKey.Trim().ToLowerInvariant()] = provider;
        }

        _byKey = dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _ordered = [.. _byKey.Values.OrderBy(static p => p.ProviderKey, StringComparer.Ordinal)];
    }

    public bool TryGet(string providerKey, out IPaymentGateway provider)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            provider = default!;
            return false;
        }

        return _byKey.TryGetValue(providerKey.Trim(), out provider!);
    }

    public IReadOnlyList<IPaymentGateway> GetAll()
    {
        return _ordered;
    }
}
