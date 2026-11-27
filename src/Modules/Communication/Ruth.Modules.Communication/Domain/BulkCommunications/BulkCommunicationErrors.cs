using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Domain errors for bulk communications, structured email composer, preferences, and two-way SMS.</summary>
public static class BulkCommunicationErrors
{
    public static readonly DomainError TemplateNameInvalid = DomainError.Validation(
        "Communication.TemplateNameInvalid",
        "Communication template name is required and cannot exceed 150 characters."
    );

    public static readonly DomainError TemplateKeyInvalid = DomainError.Validation(
        "Communication.TemplateKeyInvalid",
        "Communication template key must be 3–100 lowercase letters, digits, dots, hyphens, or underscores starting with a letter."
    );

    public static readonly DomainError TemplateKeyDuplicate = DomainError.Conflict(
        "Communication.TemplateKeyDuplicate",
        "A communication template with the specified key already exists."
    );

    public static readonly DomainError TemplateNotFound = DomainError.NotFound(
        "Communication.TemplateNotFound",
        "Communication template was not found."
    );

    public static readonly DomainError SystemTemplateProtected = DomainError.Conflict(
        "Communication.SystemTemplateProtected",
        "System communication templates cannot be deleted."
    );

    public static readonly DomainError EmailSubjectRequired = DomainError.Validation(
        "Communication.EmailSubjectRequired",
        "Email communications and templates require a subject line of 1–500 characters."
    );

    public static readonly DomainError ChannelContentRequired = DomainError.Validation(
        "Communication.ChannelContentRequired",
        "Communication body content is required for the selected channel."
    );

    public static readonly DomainError EmailSectionInvalid = DomainError.Validation(
        "Communication.EmailSectionInvalid",
        "One or more structured email sections contain invalid URLs, missing alt text, or exceed maximum section limits."
    );

    public static readonly DomainError CommunicationTitleInvalid = DomainError.Validation(
        "Communication.CommunicationTitleInvalid",
        "Communication title is required and cannot exceed 200 characters."
    );

    public static readonly DomainError TopicKeyInvalid = DomainError.Validation(
        "Communication.TopicKeyInvalid",
        "Topic key must be 2–80 lowercase letters, digits, dots, hyphens, or underscores."
    );

    public static readonly DomainError CommunicationNotFound = DomainError.NotFound(
        "Communication.CommunicationNotFound",
        "Bulk communication campaign was not found."
    );

    public static readonly DomainError CommunicationNotEditable = DomainError.Conflict(
        "Communication.CommunicationNotEditable",
        "Only Draft or Rejected communications can be edited."
    );

    public static readonly DomainError InvalidLifecycleTransition = DomainError.Conflict(
        "Communication.InvalidLifecycleTransition",
        "The requested lifecycle state transition is not valid from the current communication status."
    );

    public static readonly DomainError RecipientsRequiredForApproval = DomainError.Validation(
        "Communication.RecipientsRequiredForApproval",
        "At least one valid recipient is required before submitting or approving a communication."
    );

    public static readonly DomainError RejectionReasonRequired = DomainError.Validation(
        "Communication.RejectionReasonRequired",
        "A non-empty rejection reason is required when returning a communication to the author."
    );

    public static readonly DomainError RecipientDestinationInvalid = DomainError.Validation(
        "Communication.RecipientDestinationInvalid",
        "Recipient destination address is invalid for the selected communication channel."
    );

    public static readonly DomainError WebhookEventsEmpty = DomainError.Validation(
        "Communication.WebhookEventsEmpty",
        "At least one webhook telemetry event is required."
    );

    public static readonly DomainError WebhookSignatureInvalid = DomainError.Unauthorized(
        "Communication.WebhookSignatureInvalid",
        "Webhook signature header is missing or invalid."
    );

    public static readonly DomainError PreferenceTokenInvalid = DomainError.Unauthorized(
        "Communication.PreferenceTokenInvalid",
        "Preference or unsubscribe token is missing, malformed, or failed signature verification."
    );

    public static readonly DomainError SmsPipelineNameInvalid = DomainError.Validation(
        "Communication.SmsPipelineNameInvalid",
        "SMS pipeline name is required and cannot exceed 120 characters."
    );

    public static readonly DomainError SmsPipelineActionsInvalid = DomainError.Validation(
        "Communication.SmsPipelineActionsInvalid",
        "SMS pipeline must define 1–30 valid actions with keywords and action targets."
    );

    public static readonly DomainError SmsPipelineNotFound = DomainError.NotFound(
        "Communication.SmsPipelineNotFound",
        "SMS keyword pipeline was not found."
    );

    public static readonly DomainError SystemSmsPipelineProtected = DomainError.Conflict(
        "Communication.SystemSmsPipelineProtected",
        "Built-in system SMS pipelines cannot be deleted."
    );

    public static readonly DomainError SmsMessageBodyInvalid = DomainError.Validation(
        "Communication.SmsMessageBodyInvalid",
        "SMS message body is required and cannot exceed 1,600 characters."
    );

    public static readonly DomainError SmsRecipientOptedOut = DomainError.Conflict(
        "Communication.SmsRecipientOptedOut",
        "Recipient phone number has opted out of SMS messages."
    );

    public static readonly DomainError TenantNotLive = DomainError.Validation(
        "Communication.TenantNotLive",
        "Outbound bulk email, drip flows, and SMS dispatch require IRS tax-exempt verification and Live workspace approval."
    );
}
