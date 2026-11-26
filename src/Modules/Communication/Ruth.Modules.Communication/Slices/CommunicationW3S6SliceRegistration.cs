using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Slices.BulkCommunications;
using Ruth.Modules.Communication.Slices.TwoWaySms;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Communication.Slices;

internal static class CommunicationW3S6SliceRegistration
{
    public static void RegisterW3S6Services(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        _ = services.Configure<CommunicationBulkOptions>(
            configuration.GetSection(CommunicationBulkOptions.SectionName)
        );

        services.TryAddScoped<BulkEmailComposerService>();
        services.TryAddScoped<BulkRecipientResolver>();
        services.TryAddScoped<PreferenceResolutionHelper>();
        services.TryAddScoped<SmsPipelineActionExecutor>();

        RegisterTemplateAndComposerHandlers(services);
        RegisterCampaignAndDispatchHandlers(services);
        RegisterWebhookAndPreferenceHandlers(services);
        RegisterTwoWaySmsHandlers(services);
        _ = services.AddScheduledJob<BulkCommunicationDispatchJob>();
    }

    private static void RegisterTemplateAndComposerHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateCommunicationTemplateCommand,
            CommunicationTemplateResponse,
            CreateCommunicationTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCommunicationTemplateCommand,
            CommunicationTemplateResponse,
            UpdateCommunicationTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteCommunicationTemplateCommand,
            bool,
            DeleteCommunicationTemplateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationTemplatesQuery,
            IReadOnlyList<CommunicationTemplateResponse>,
            GetCommunicationTemplatesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationTemplateByIdQuery,
            CommunicationTemplateResponse,
            GetCommunicationTemplateByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            PreviewEmailComposerCommand,
            EmailComposerPreviewResponse,
            PreviewEmailComposerCommandHandler
        >();
    }

    private static void RegisterCampaignAndDispatchHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateCommunicationCommand,
            CommunicationDetailResponse,
            CreateCommunicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCommunicationDraftCommand,
            CommunicationDetailResponse,
            UpdateCommunicationDraftCommandHandler
        >();
        _ = services.AddCommandHandler<
            SubmitCommunicationForApprovalCommand,
            CommunicationDetailResponse,
            SubmitCommunicationForApprovalCommandHandler
        >();
        _ = services.AddCommandHandler<
            ApproveCommunicationCommand,
            CommunicationDetailResponse,
            ApproveCommunicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            RejectCommunicationCommand,
            CommunicationDetailResponse,
            RejectCommunicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelCommunicationCommand,
            CommunicationDetailResponse,
            CancelCommunicationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationsQuery,
            IReadOnlyList<CommunicationSummaryResponse>,
            GetCommunicationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommunicationByIdQuery,
            CommunicationDetailResponse,
            GetCommunicationByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            ProcessDueBulkCommunicationsCommand,
            BulkDispatchBatchResultResponse,
            ProcessDueBulkCommunicationsCommandHandler
        >();
    }

    private static void RegisterWebhookAndPreferenceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            ProcessCommunicationWebhooksCommand,
            CommunicationWebhookProcessResponse,
            ProcessCommunicationWebhooksCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRecipientPreferencesQuery,
            CommunicationPreferenceResponse,
            GetRecipientPreferencesQueryHandler
        >();
        _ = services.AddCommandHandler<
            UpdateRecipientPreferencesCommand,
            CommunicationPreferenceResponse,
            UpdateRecipientPreferencesCommandHandler
        >();
        _ = services.AddCommandHandler<
            OneClickUnsubscribeCommand,
            CommunicationPreferenceResponse,
            OneClickUnsubscribeCommandHandler
        >();
    }

    private static void RegisterTwoWaySmsHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateSmsPipelineCommand,
            SmsPipelineResponse,
            CreateSmsPipelineCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateSmsPipelineCommand,
            SmsPipelineResponse,
            UpdateSmsPipelineCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteSmsPipelineCommand,
            bool,
            DeleteSmsPipelineCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSmsPipelinesQuery,
            IReadOnlyList<SmsPipelineResponse>,
            GetSmsPipelinesQueryHandler
        >();
        _ = services.AddCommandHandler<
            ProcessInboundSmsWebhookCommand,
            InboundSmsProcessResponse,
            ProcessInboundSmsWebhookCommandHandler
        >();
        _ = services.AddCommandHandler<
            SendSmsConversationReplyCommand,
            SmsConversationMessageResponse,
            SendSmsConversationReplyCommandHandler
        >();
        _ = services.AddCommandHandler<
            MarkSmsConversationReadCommand,
            int,
            MarkSmsConversationReadCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSmsConversationsQuery,
            IReadOnlyList<SmsConversationSummaryResponse>,
            GetSmsConversationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetSmsConversationThreadQuery,
            IReadOnlyList<SmsConversationMessageResponse>,
            GetSmsConversationThreadQueryHandler
        >();
    }
}
