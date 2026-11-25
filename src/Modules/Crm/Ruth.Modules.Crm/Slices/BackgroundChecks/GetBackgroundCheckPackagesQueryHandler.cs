using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.BackgroundChecks;

internal sealed class GetBackgroundCheckPackagesQueryHandler(
    IBackgroundCheckProviderResolver providerResolver
) : IQueryHandler<GetBackgroundCheckPackagesQuery, IReadOnlyList<BackgroundCheckPackageResponse>>
{
    public async Task<Result<IReadOnlyList<BackgroundCheckPackageResponse>>> HandleAsync(
        GetBackgroundCheckPackagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrWhiteSpace(query.ProviderKey))
        {
            var provider = providerResolver.Resolve(query.ProviderKey);
            return provider is null
                ? CrmErrors.BackgroundCheckProviderNotFound
                : await provider.GetPackagesAsync(cancellationToken);
        }

        var all = new List<BackgroundCheckPackageResponse>();
        foreach (var provider in providerResolver.GetAll())
        {
            var packages = await provider.GetPackagesAsync(cancellationToken);
            if (packages.IsSuccess)
            {
                all.AddRange(packages.Value);
            }
        }

        return Result.Success<IReadOnlyList<BackgroundCheckPackageResponse>>(all);
    }
}
