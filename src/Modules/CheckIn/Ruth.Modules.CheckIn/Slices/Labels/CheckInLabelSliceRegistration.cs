using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Infrastructure.Printing;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal static class CheckInLabelSliceRegistration
{
    public static void RegisterHandlers(IServiceCollection services)
    {
        services.TryAddSingleton<IZplPrinterDispatcher, TcpRawSocketZplPrinterDispatcher>();
        _ = services.AddScoped<SessionLabelPrintCollaborator>();

        _ = services.AddCommandHandler<
            CreateCheckInLabelCommand,
            CheckInLabelResponse,
            CreateCheckInLabelCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCheckInLabelCommand,
            CheckInLabelResponse,
            UpdateCheckInLabelCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteCheckInLabelCommand,
            bool,
            DeleteCheckInLabelCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCheckInLabelsQuery,
            IReadOnlyList<CheckInLabelResponse>,
            GetCheckInLabelsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCheckInLabelByIdQuery,
            CheckInLabelResponse,
            GetCheckInLabelByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            PreviewCheckInLabelCommand,
            CheckInLabelPreviewResponse,
            PreviewCheckInLabelCommandHandler
        >();
        _ = services.AddCommandHandler<
            PrintCheckInSessionLabelsCommand,
            PrintCheckInSessionLabelsResponse,
            PrintCheckInSessionLabelsCommandHandler
        >();
    }
}
