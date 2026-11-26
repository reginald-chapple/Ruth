using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal static class FinanceModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyCoreLedgerConfigurations(modelBuilder);
        ApplyStatementAssistanceAndCampaignConfigurations(modelBuilder);
    }

    private static void ApplyCoreLedgerConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new FinancialAccountEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialPledgeEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialGatewayEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SavedPaymentMethodEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BankAccountIdentifierEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InvoiceEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InvoiceLineItemEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialBatchEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialTransactionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialTransactionDetailEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialTransactionImageEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FinancialTransactionRefundEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RecurringScheduleTransactionEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RecurringScheduleSplitEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ProcessedGatewayWebhookEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TransactionCadenceAlertTypeEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TransactionCadenceAlertEntityConfiguration());
    }

    private static void ApplyStatementAssistanceAndCampaignConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new FinancialStatementTemplateEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new GeneratedFinancialStatementEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssistanceProgramEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssistanceCaseEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssistanceDocumentEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssistanceDisbursementEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FundraisingCampaignEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CampaignParticipantEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CampaignContributionCreditEntityConfiguration());
    }
}
