using Microsoft.Extensions.DependencyInjection;

namespace Ruth.Modules.Cms.Slices;

internal static class CmsSlicesRegistration
{
    public static void Register(IServiceCollection services)
    {
        W6S1CmsEngineRegistration.Register(services);
        W6S2CmsContentRegistration.Register(services);
        W6S3CmsPersonalizationRegistration.Register(services);
        W6S4CmsChannelsRegistration.Register(services);
    }
}
