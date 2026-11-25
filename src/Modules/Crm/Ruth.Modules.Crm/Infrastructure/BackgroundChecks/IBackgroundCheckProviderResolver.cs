using Ruth.Modules.Crm.Contracts.BackgroundChecks;

namespace Ruth.Modules.Crm.Infrastructure.BackgroundChecks;

internal interface IBackgroundCheckProviderResolver
{
    IReadOnlyList<IBackgroundCheckProvider> GetAll();

    IBackgroundCheckProvider? Resolve(string providerKey);
}
