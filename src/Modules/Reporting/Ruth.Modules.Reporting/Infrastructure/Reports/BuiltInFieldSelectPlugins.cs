using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.Reports;

namespace Ruth.Modules.Reporting.Infrastructure.Reports;

internal static class BuiltInFieldSelectPlugins
{
    public static void Register(IServiceCollection services)
    {
        _ = services.AddSingleton<IFieldSelectPlugin, EntityPropertyFieldSelectPlugin>();
        _ = services.AddSingleton<IFieldSelectPlugin, JsonbAttributeFieldSelectPlugin>();
        _ = services.AddSingleton<IFieldSelectPlugin, CompositeFormatFieldSelectPlugin>();
        _ = services.AddSingleton<IFieldSelectPlugin, DateMonthFieldSelectPlugin>();
    }
}
