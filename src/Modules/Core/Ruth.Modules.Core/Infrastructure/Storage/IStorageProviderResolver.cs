using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Modules.Core.Infrastructure.Storage;

internal interface IStorageProviderResolver
{
    IStorageProvider? Resolve(string providerKey);
}
