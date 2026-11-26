using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure.WalletPasses;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal static class WalletPassSliceRegistration
{
    public static void RegisterHandlers(IServiceCollection services)
    {
        _ = services.AddSingleton<IWalletPassProvider, PreviewWalletPassProvider>();
        services.TryAddSingleton<InMemoryWalletPassPushNotifier>();
        services.TryAddSingleton<IWalletPassPushNotifier>(static sp =>
            sp.GetRequiredService<InMemoryWalletPassPushNotifier>()
        );

        _ = services.AddScoped<WalletPassBundleComposer>();
        _ = services.AddScoped<WalletPassPushDispatchCollaborator>();

        RegisterTemplateAndIssuanceHandlers(services);
        RegisterPassKitAndScanHandlers(services);
    }

    private static void RegisterTemplateAndIssuanceHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<
            ICommandHandler<CreateWalletPassTemplateCommand, WalletPassTemplateResponse>,
            CreateWalletPassTemplateCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<UpdateWalletPassTemplateCommand, WalletPassTemplateResponse>,
            UpdateWalletPassTemplateCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<DeleteWalletPassTemplateCommand>,
            DeleteWalletPassTemplateCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetWalletPassTemplatesQuery, IReadOnlyList<WalletPassTemplateResponse>>,
            GetWalletPassTemplatesQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<IssueWalletPassCommand, IssueWalletPassResultResponse>,
            IssueWalletPassCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<UpdateIssuedWalletPassDynamicFieldsCommand, IssuedWalletPassResponse>,
            UpdateIssuedWalletPassDynamicFieldsCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<RevokeIssuedWalletPassCommand, IssuedWalletPassResponse>,
            RevokeIssuedWalletPassCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetIssuedWalletPassesQuery, IReadOnlyList<IssuedWalletPassResponse>>,
            GetIssuedWalletPassesQueryHandler
        >();
    }

    private static void RegisterPassKitAndScanHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<
            IQueryHandler<ClaimWalletPassQuery, WalletPassClaimBundleResponse>,
            ClaimWalletPassQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<PreviewIssuedWalletPassQuery, WalletPassClaimBundleResponse>,
            PreviewIssuedWalletPassQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<ResolveKioskWalletPassScanQuery, KioskWalletPassScanResponse>,
            ResolveKioskWalletPassScanQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<RegisterPassKitDeviceCommand, bool>,
            RegisterPassKitDeviceCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<UnregisterPassKitDeviceCommand>,
            UnregisterPassKitDeviceCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetPassKitUpdatableSerialsQuery, PassKitSerialNumbersResponse>,
            GetPassKitUpdatableSerialsQueryHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetPassKitLatestPassQuery, PassKitLatestPassResponse>,
            GetPassKitLatestPassQueryHandler
        >();
    }
}
