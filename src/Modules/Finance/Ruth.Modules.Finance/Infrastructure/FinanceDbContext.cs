using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Finance.Infrastructure;

/// <summary>EF Core database context exclusively owning the finance PostgreSQL schema.</summary>
public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Finance module.</summary>
    public const string SchemaName = "finance";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the hierarchical financial accounts and funds table.</summary>
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    /// <summary>Gets the constituent and corporate financial pledges table.</summary>
    public DbSet<FinancialPledge> FinancialPledges => Set<FinancialPledge>();

    /// <summary>Gets the configured financial payment gateways table.</summary>
    public DbSet<FinancialGateway> FinancialGateways => Set<FinancialGateway>();

    /// <summary>Gets the tokenized constituent saved payment methods table.</summary>
    public DbSet<SavedPaymentMethod> SavedPaymentMethods => Set<SavedPaymentMethod>();

    /// <summary>Gets the keyed HMAC-SHA256 bank routing/account identifiers table.</summary>
    public DbSet<BankAccountIdentifier> BankAccountIdentifiers => Set<BankAccountIdentifier>();

    /// <summary>Gets the membership dues, fee, rental, and sponsorship invoices table.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Gets the invoice line items table.</summary>
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    /// <summary>Gets the financial settlement batches table.</summary>
    public DbSet<FinancialBatch> FinancialBatches => Set<FinancialBatch>();

    /// <summary>Gets the financial transactions table.</summary>
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();

    /// <summary>Gets the financial transaction account splits table.</summary>
    public DbSet<FinancialTransactionDetail> FinancialTransactionDetails =>
        Set<FinancialTransactionDetail>();

    /// <summary>Gets the AES-256-GCM encrypted check/remittance image metadata table.</summary>
    public DbSet<FinancialTransactionImage> FinancialTransactionImages =>
        Set<FinancialTransactionImage>();

    /// <summary>Gets the financial transaction refunds table.</summary>
    public DbSet<FinancialTransactionRefund> FinancialTransactionRefunds =>
        Set<FinancialTransactionRefund>();

    /// <summary>Gets the recurring contribution and membership subscription schedules table.</summary>
    public DbSet<RecurringScheduleTransaction> RecurringSchedules =>
        Set<RecurringScheduleTransaction>();

    /// <summary>Gets the recurring schedule account splits table.</summary>
    public DbSet<RecurringScheduleSplit> RecurringScheduleSplits => Set<RecurringScheduleSplit>();

    /// <summary>Gets the idempotent payment gateway webhook log table.</summary>
    public DbSet<ProcessedGatewayWebhook> ProcessedGatewayWebhooks =>
        Set<ProcessedGatewayWebhook>();

    /// <summary>Gets the statistical transaction cadence alert rule definitions table.</summary>
    public DbSet<TransactionCadenceAlertType> CadenceAlertTypes =>
        Set<TransactionCadenceAlertType>();

    /// <summary>Gets the detected constituent lapse/churn and unusual spike alerts table.</summary>
    public DbSet<TransactionCadenceAlert> CadenceAlerts => Set<TransactionCadenceAlert>();

    /// <summary>Gets the configured financial statement and tax receipt templates table.</summary>
    public DbSet<FinancialStatementTemplate> StatementTemplates =>
        Set<FinancialStatementTemplate>();

    /// <summary>Gets the generated constituent, household, and organization statements table.</summary>
    public DbSet<GeneratedFinancialStatement> GeneratedStatements =>
        Set<GeneratedFinancialStatement>();

    /// <summary>Gets the assistance, scholarship, fee-waiver, and micro-grant programs table.</summary>
    public DbSet<AssistanceProgram> AssistancePrograms => Set<AssistanceProgram>();

    /// <summary>Gets the assistance and grant cases table.</summary>
    public DbSet<AssistanceCase> AssistanceCases => Set<AssistanceCase>();

    /// <summary>Gets the assistance case verification documents table.</summary>
    public DbSet<AssistanceDocument> AssistanceDocuments => Set<AssistanceDocument>();

    /// <summary>Gets the assistance case disbursements and fee-waiver credits table.</summary>
    public DbSet<AssistanceDisbursement> AssistanceDisbursements => Set<AssistanceDisbursement>();

    /// <summary>Gets the peer-to-peer, team, and project fundraising campaigns table.</summary>
    public DbSet<FundraisingCampaign> FundraisingCampaigns => Set<FundraisingCampaign>();

    /// <summary>Gets the campaign participant profiles and personal goal overrides table.</summary>
    public DbSet<CampaignParticipant> CampaignParticipants => Set<CampaignParticipant>();

    /// <summary>Gets the campaign contribution credits and donor privacy settings table.</summary>
    public DbSet<CampaignContributionCredit> CampaignContributionCredits =>
        Set<CampaignContributionCredit>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.FinanceModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
