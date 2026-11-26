using Ruth.Modules.Finance.Domain;
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
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Finance;

internal static class FinanceEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            FinanceSystemGuids.FinancialAccount,
            FinancialAccount.EntityTypeKeyName,
            nameof(FinancialAccount),
            "Financial Account / Fund",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            FinanceSystemGuids.FinancialPledge,
            "Ruth.Finance.FinancialPledge",
            nameof(FinancialPledge),
            "Financial Pledge",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.FinancialGateway,
            FinancialGateway.EntityTypeKeyName,
            nameof(FinancialGateway),
            "Financial Payment Gateway",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.SavedPaymentMethod,
            "Ruth.Finance.SavedPaymentMethod",
            nameof(SavedPaymentMethod),
            "Tokenized Saved Payment Method",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.BankAccountIdentifier,
            "Ruth.Finance.BankAccountIdentifier",
            nameof(BankAccountIdentifier),
            "Hashed Bank Account Identifier",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.Invoice,
            "Ruth.Finance.Invoice",
            nameof(Invoice),
            "Invoice",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.FinancialBatch,
            FinancialBatch.EntityTypeKeyName,
            nameof(FinancialBatch),
            "Financial Settlement Batch",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.FinancialTransaction,
            "Ruth.Finance.FinancialTransaction",
            nameof(FinancialTransaction),
            "Financial Transaction",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.RecurringScheduleTransaction,
            "Ruth.Finance.RecurringScheduleTransaction",
            nameof(RecurringScheduleTransaction),
            "Recurring Schedule Transaction",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.TransactionCadenceAlertType,
            "Ruth.Finance.TransactionCadenceAlertType",
            nameof(TransactionCadenceAlertType),
            "Transaction Cadence Alert Type",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.TransactionCadenceAlert,
            "Ruth.Finance.TransactionCadenceAlert",
            nameof(TransactionCadenceAlert),
            "Transaction Cadence Alert",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.FinancialStatementTemplate,
            "Ruth.Finance.FinancialStatementTemplate",
            nameof(FinancialStatementTemplate),
            "Financial Statement Template",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.GeneratedFinancialStatement,
            "Ruth.Finance.GeneratedFinancialStatement",
            nameof(GeneratedFinancialStatement),
            "Generated Financial Statement",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.AssistanceProgram,
            AssistanceProgram.EntityTypeKeyName,
            nameof(AssistanceProgram),
            "Assistance & Grant Program",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            FinanceSystemGuids.AssistanceCase,
            AssistanceCase.EntityTypeKeyName,
            nameof(AssistanceCase),
            "Assistance & Grant Case",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            FinanceSystemGuids.FundraisingCampaign,
            FundraisingCampaign.EntityTypeKeyName,
            nameof(FundraisingCampaign),
            "Fundraising Campaign",
            isSecured: true,
            supportsAttributes: true
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Finance",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
