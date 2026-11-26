using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Infrastructure.Statements;
using Ruth.Modules.Finance.Slices.Assistance;
using Ruth.Modules.Finance.Slices.Statements;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Finance.Slices;

internal static class FinanceW4S5SliceRegistration
{
    public static void RegisterW4S5Services(IServiceCollection services)
    {
        services.TryAddScoped<IFinancialStatementRenderer, FinancialStatementRenderer>();
        services.TryAddScoped<StatementRecipientResolver>();
        services.TryAddScoped<StatementLedgerQueryService>();
        services.TryAddScoped<StatementGenerationCollaborator>();

        RegisterStatementHandlers(services);
        RegisterAssistanceHandlers(services);
    }

    private static void RegisterStatementHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateFinancialStatementTemplateCommand,
            FinancialStatementTemplateResponse,
            CreateFinancialStatementTemplateCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateFinancialStatementTemplateCommand,
            FinancialStatementTemplateResponse,
            UpdateFinancialStatementTemplateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialStatementTemplatesQuery,
            IReadOnlyList<FinancialStatementTemplateResponse>,
            GetFinancialStatementTemplatesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetFinancialStatementTemplateByIdQuery,
            FinancialStatementTemplateResponse,
            GetFinancialStatementTemplateByIdQueryHandler
        >();
        _ = services.AddCommandHandler<
            GenerateFinancialStatementCommand,
            RenderedFinancialStatementDocumentResponse,
            GenerateFinancialStatementCommandHandler
        >();
        _ = services.AddCommandHandler<
            RunFinancialStatementBatchCommand,
            FinancialStatementBatchRunResponse,
            RunFinancialStatementBatchCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetGeneratedFinancialStatementsQuery,
            IReadOnlyList<GeneratedFinancialStatementResponse>,
            GetGeneratedFinancialStatementsQueryHandler
        >();
        _ = services.AddQueryHandler<
            DownloadFinancialStatementQuery,
            FinancialStatementDownloadResponse,
            DownloadFinancialStatementQueryHandler
        >();
    }

    private static void RegisterAssistanceHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateAssistanceProgramCommand,
            AssistanceProgramResponse,
            CreateAssistanceProgramCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateAssistanceProgramCommand,
            AssistanceProgramResponse,
            UpdateAssistanceProgramCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAssistanceProgramsQuery,
            IReadOnlyList<AssistanceProgramResponse>,
            GetAssistanceProgramsQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitAssistanceCaseCommand,
            AssistanceCaseResponse,
            SubmitAssistanceCaseCommandHandler
        >();
        _ = services.AddCommandHandler<
            AttachAssistanceDocumentCommand,
            AssistanceCaseResponse,
            AttachAssistanceDocumentCommandHandler
        >();
        _ = services.AddCommandHandler<
            ReviewAssistanceCaseCommand,
            AssistanceCaseResponse,
            ReviewAssistanceCaseCommandHandler
        >();
        _ = services.AddCommandHandler<
            RecordAssistanceDisbursementCommand,
            AssistanceCaseResponse,
            RecordAssistanceDisbursementCommandHandler
        >();
        _ = services.AddCommandHandler<
            CloseAssistanceCaseCommand,
            AssistanceCaseResponse,
            CloseAssistanceCaseCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAssistanceCasesQuery,
            IReadOnlyList<AssistanceCaseResponse>,
            GetAssistanceCasesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetAssistanceCaseByIdQuery,
            AssistanceCaseResponse,
            GetAssistanceCaseByIdQueryHandler
        >();
    }
}
