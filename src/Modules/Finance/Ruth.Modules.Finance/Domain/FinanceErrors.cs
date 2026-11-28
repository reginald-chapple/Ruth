using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain;

internal static class FinanceErrors
{
    public static readonly DomainError CurrencyCodeInvalid = DomainError.Validation(
        "Finance.Money.CurrencyCodeInvalid",
        "Currency code must be a supported 3-letter uppercase ISO-4217 currency code."
    );

    public static readonly DomainError CurrencyScaleInvalid = DomainError.Validation(
        "Finance.Money.CurrencyScaleInvalid",
        "Monetary amount exceeds the minor-unit decimal precision allowed for its ISO-4217 currency."
    );

    public static readonly DomainError CurrencyMismatch = DomainError.Validation(
        "Finance.Money.CurrencyMismatch",
        "Monetary operations require matching ISO-4217 currency codes."
    );

    public static readonly DomainError AmountMustBePositive = DomainError.Validation(
        "Finance.Money.AmountMustBePositive",
        "Monetary amount must be strictly greater than zero."
    );

    public static readonly DomainError AmountMustBeNonNegative = DomainError.Validation(
        "Finance.Money.AmountMustBeNonNegative",
        "Monetary amount cannot be negative."
    );

    public static readonly DomainError AllocationWeightsInvalid = DomainError.Validation(
        "Finance.Money.AllocationWeightsInvalid",
        "Allocation weights must contain at least one positive weight and no negative weights."
    );

    public static readonly DomainError AccountNameInvalid = DomainError.Validation(
        "Finance.Account.NameInvalid",
        "Financial account name is required and cannot exceed 160 characters."
    );

    public static readonly DomainError AccountCodeInvalid = DomainError.Validation(
        "Finance.Account.CodeInvalid",
        "Financial account code must be 2 to 64 uppercase letters, digits, underscores, or hyphens."
    );

    public static readonly DomainError AccountCodeDuplicate = DomainError.Conflict(
        "Finance.Account.CodeDuplicate",
        "Another financial account already uses this account code."
    );

    public static readonly DomainError AccountDateRangeInvalid = DomainError.Validation(
        "Finance.Account.DateRangeInvalid",
        "Financial account end date cannot precede its start date."
    );

    public static readonly DomainError AccountNotFound = DomainError.NotFound(
        "Finance.Account.NotFound",
        "The requested financial account was not found."
    );

    public static readonly DomainError AccountHierarchyCycle = DomainError.Validation(
        "Finance.Account.HierarchyCycle",
        "A financial account cannot be assigned as its own parent or descendant."
    );

    public static readonly DomainError AccountClosedForDate = DomainError.Validation(
        "Finance.Account.ClosedForDate",
        "The target financial account is inactive or outside its open posting window."
    );

    public static readonly DomainError OrganizationNameInvalid = DomainError.Validation(
        "Finance.Organization.NameInvalid",
        "Organization name is required and cannot exceed 160 characters."
    );

    public static readonly DomainError OrganizationNotFound = DomainError.NotFound(
        "Finance.Organization.NotFound",
        "The requested organization or corporate sponsor record was not found."
    );

    public static readonly DomainError PledgePersonRequired = DomainError.Validation(
        "Finance.Pledge.PersonRequired",
        "A financial pledge requires a valid constituent or organization Person identifier."
    );

    public static readonly DomainError PledgeDateRangeInvalid = DomainError.Validation(
        "Finance.Pledge.DateRangeInvalid",
        "Financial pledge end date cannot precede its start date."
    );

    public static readonly DomainError PledgeNotFound = DomainError.NotFound(
        "Finance.Pledge.NotFound",
        "The requested financial pledge was not found."
    );

    public static readonly DomainError PledgeNotEditable = DomainError.Conflict(
        "Finance.Pledge.NotEditable",
        "Only active financial pledges can be modified or cancelled."
    );

    public static readonly DomainError GatewayNameInvalid = DomainError.Validation(
        "Finance.Gateway.NameInvalid",
        "Financial gateway name is required and cannot exceed 120 characters."
    );

    public static readonly DomainError GatewayProviderKeyInvalid = DomainError.Validation(
        "Finance.Gateway.ProviderKeyInvalid",
        "Gateway provider key must be 2 to 64 lowercase letters, digits, hyphens, or underscores."
    );

    public static readonly DomainError GatewayBatchOffsetInvalid = DomainError.Validation(
        "Finance.Gateway.BatchOffsetInvalid",
        "Gateway batch time offset hours must be between -12 and 14."
    );

    public static readonly DomainError GatewayNotFound = DomainError.NotFound(
        "Finance.Gateway.NotFound",
        "The requested financial gateway was not found."
    );

    public static readonly DomainError GatewayInactive = DomainError.Conflict(
        "Finance.Gateway.Inactive",
        "The selected financial gateway is inactive."
    );

    public static readonly DomainError GatewayProviderUnavailable = DomainError.Failure(
        "Finance.Gateway.ProviderUnavailable",
        "No registered IPaymentGateway implementation matched the gateway provider key."
    );

    public static readonly DomainError LiveChargeRequiresApprovedTenant = DomainError.Validation(
        "Finance.LiveChargeRequiresApprovedTenant",
        "Live payment gateway charges require IRS tax-exempt verification and Live workspace approval."
    );

    public static readonly DomainError PaymentMethodPersonRequired = DomainError.Validation(
        "Finance.PaymentMethod.PersonRequired",
        "A saved payment method requires a valid Person identifier."
    );

    public static readonly DomainError PaymentMethodVaultTokenInvalid = DomainError.Validation(
        "Finance.PaymentMethod.VaultTokenInvalid",
        "Customer vault token is required and cannot contain a raw card PAN."
    );

    public static readonly DomainError PaymentMethodMaskedNumberInvalid = DomainError.Validation(
        "Finance.PaymentMethod.MaskedNumberInvalid",
        "Masked account number must retain only up to 4 trailing visible characters and never raw PAN digits."
    );

    public static readonly DomainError PaymentMethodExpiryInvalid = DomainError.Validation(
        "Finance.PaymentMethod.ExpiryInvalid",
        "Card payment methods require a valid expiration month (1..12) and four-digit year."
    );

    public static readonly DomainError PaymentMethodNotFound = DomainError.NotFound(
        "Finance.PaymentMethod.NotFound",
        "The requested saved payment method was not found."
    );

    public static readonly DomainError BankRoutingChecksumInvalid = DomainError.Validation(
        "Finance.BankIdentifier.RoutingInvalid",
        "Routing number must be a valid 9-digit ABA routing number passing checksum verification."
    );

    public static readonly DomainError BankAccountNumberInvalid = DomainError.Validation(
        "Finance.BankIdentifier.AccountNumberInvalid",
        "Bank account number must contain 4 to 17 digits."
    );

    public static readonly DomainError InvoiceLineItemsRequired = DomainError.Validation(
        "Finance.Invoice.LineItemsRequired",
        "An invoice requires at least one line item with positive quantity and total."
    );

    public static readonly DomainError InvoiceDateRangeInvalid = DomainError.Validation(
        "Finance.Invoice.DateRangeInvalid",
        "Invoice due date cannot precede its issue date."
    );

    public static readonly DomainError InvoiceNotFound = DomainError.NotFound(
        "Finance.Invoice.NotFound",
        "The requested invoice was not found."
    );

    public static readonly DomainError InvoicePaymentInvalid = DomainError.Conflict(
        "Finance.Invoice.PaymentInvalid",
        "Invoice payment must target an open or partially paid invoice and cannot exceed balance due."
    );

    public static readonly DomainError InvoiceVoidNotAllowed = DomainError.Conflict(
        "Finance.Invoice.VoidNotAllowed",
        "Invoices with settled payments cannot be voided until payments are refunded."
    );

    public static readonly DomainError BatchNameInvalid = DomainError.Validation(
        "Finance.Batch.NameInvalid",
        "Financial batch name is required and cannot exceed 160 characters."
    );

    public static readonly DomainError BatchNotFound = DomainError.NotFound(
        "Finance.Batch.NotFound",
        "The requested financial batch was not found."
    );

    public static readonly DomainError BatchAuditLocked = DomainError.Conflict(
        "Finance.Batch.AuditLocked",
        "Closed financial batches are audit-locked and cannot be modified without explicit reopening."
    );

    public static readonly DomainError BatchInvalidTransition = DomainError.Conflict(
        "Finance.Batch.InvalidTransition",
        "Financial batch state transition is not permitted from its current status."
    );

    public static readonly DomainError BatchControlVariance = DomainError.Conflict(
        "Finance.Batch.ControlVariance",
        "Financial batch cannot be closed until actual transaction totals and counts match control values."
    );

    public static readonly DomainError TransactionSplitsInvalid = DomainError.Validation(
        "Finance.Transaction.SplitsInvalid",
        "Financial transaction requires one or more positive account splits summing to the total amount."
    );

    public static readonly DomainError TransactionNotFound = DomainError.NotFound(
        "Finance.Transaction.NotFound",
        "The requested financial transaction was not found."
    );

    public static readonly DomainError RefundExceedsSettledAmount = DomainError.Conflict(
        "Finance.Transaction.RefundExceedsTotal",
        "Cumulative refund amount cannot exceed the settled transaction total."
    );

    public static readonly DomainError CheckImageInvalid = DomainError.Validation(
        "Finance.CheckImage.Invalid",
        "Check or remittance image content is empty, oversized, or failed authenticated decryption."
    );

    public static readonly DomainError RecurringScheduleInvalid = DomainError.Validation(
        "Finance.Recurring.Invalid",
        "Recurring schedule requires valid dates, active payment method, and positive account splits."
    );

    public static readonly DomainError RecurringScheduleNotFound = DomainError.NotFound(
        "Finance.Recurring.NotFound",
        "The requested recurring schedule was not found."
    );

    public static readonly DomainError RecurringScheduleStateConflict = DomainError.Conflict(
        "Finance.Recurring.StateConflict",
        "The requested recurring schedule transition is not valid in its current status."
    );

    public static readonly DomainError CadenceAlertTypeInvalid = DomainError.Validation(
        "Finance.CadenceAlertType.Invalid",
        "Cadence alert type configuration thresholds and name must be valid and positive."
    );

    public static readonly DomainError CadenceAlertNotFound = DomainError.NotFound(
        "Finance.CadenceAlert.NotFound",
        "The requested transaction cadence alert was not found."
    );

    public static readonly DomainError StatementTemplateInvalid = DomainError.Validation(
        "Finance.StatementTemplate.Invalid",
        "Statement template requires valid name, organization display name, safe logo URL, and non-negative threshold."
    );

    public static readonly DomainError StatementTemplateUnsafeCss = DomainError.Validation(
        "Finance.StatementTemplate.UnsafeCss",
        "Custom print CSS cannot contain script tags, external @import directives, or javascript expressions."
    );

    public static readonly DomainError StatementTemplateNotFound = DomainError.NotFound(
        "Finance.StatementTemplate.NotFound",
        "The requested financial statement template was not found."
    );

    public static readonly DomainError StatementPeriodInvalid = DomainError.Validation(
        "Finance.Statement.PeriodInvalid",
        "Statement period end date cannot precede its start date."
    );

    public static readonly DomainError StatementRecipientInvalid = DomainError.Validation(
        "Finance.Statement.RecipientInvalid",
        "Statement generation requires a valid target person, household, or organization identifier."
    );

    public static readonly DomainError StatementNotFound = DomainError.NotFound(
        "Finance.Statement.NotFound",
        "The requested generated financial statement was not found."
    );

    public static readonly DomainError AssistanceProgramInvalid = DomainError.Validation(
        "Finance.AssistanceProgram.Invalid",
        "Assistance program requires a valid name, code, funding account, positive budget, and valid award ceiling."
    );

    public static readonly DomainError AssistanceProgramCodeDuplicate = DomainError.Conflict(
        "Finance.AssistanceProgram.CodeDuplicate",
        "Another assistance program already uses this program code."
    );

    public static readonly DomainError AssistanceProgramNotFound = DomainError.NotFound(
        "Finance.AssistanceProgram.NotFound",
        "The requested assistance program was not found."
    );

    public static readonly DomainError AssistanceBudgetExceeded = DomainError.Conflict(
        "Finance.Assistance.BudgetExceeded",
        "The requested award or disbursement exceeds the program's remaining budget or per-case ceiling."
    );

    public static readonly DomainError AssistanceCaseInvalid = DomainError.Validation(
        "Finance.AssistanceCase.Invalid",
        "Assistance case requires a valid program, applicant person, summary, and positive requested amount."
    );

    public static readonly DomainError AssistanceCaseNotFound = DomainError.NotFound(
        "Finance.AssistanceCase.NotFound",
        "The requested assistance case was not found."
    );

    public static readonly DomainError AssistanceCaseInvalidTransition = DomainError.Conflict(
        "Finance.AssistanceCase.InvalidTransition",
        "The requested assistance case transition is not valid from its current status."
    );

    public static readonly DomainError AssistanceDocumentationRequired = DomainError.Conflict(
        "Finance.AssistanceCase.DocumentationRequired",
        "This assistance program requires at least one verified supporting document before case approval."
    );

    public static readonly DomainError AssistanceDocumentInvalid = DomainError.Validation(
        "Finance.AssistanceDocument.Invalid",
        "Assistance document requires a non-empty title and document category."
    );

    public static readonly DomainError AssistanceDisbursementInvalid = DomainError.Validation(
        "Finance.AssistanceDisbursement.Invalid",
        "Assistance disbursement requires a positive amount and payee name."
    );

    public static readonly DomainError AssistanceDisbursementExceedsApproved = DomainError.Conflict(
        "Finance.AssistanceDisbursement.ExceedsApproved",
        "Cumulative disbursements cannot exceed the approved award ceiling on the case."
    );
}
