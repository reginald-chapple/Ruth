using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Domain error catalog for automated CommunicationFlow drip campaigns and conversions.</summary>
public static class CommunicationFlowErrors
{
    public static readonly DomainError FlowNameInvalid = DomainError.Validation(
        "Communication.FlowNameInvalid",
        "Communication flow name is required and cannot exceed 150 characters."
    );

    public static readonly DomainError FlowKeyInvalid = DomainError.Validation(
        "Communication.FlowKeyInvalid",
        "Communication flow key must be 3–100 lowercase letters, digits, dots, hyphens, or underscores starting with a letter."
    );

    public static readonly DomainError FlowKeyDuplicate = DomainError.Conflict(
        "Communication.FlowKeyDuplicate",
        "A communication flow with the specified key already exists."
    );

    public static readonly DomainError FlowNotFound = DomainError.NotFound(
        "Communication.FlowNotFound",
        "Communication flow was not found."
    );

    public static readonly DomainError FlowInactive = DomainError.Conflict(
        "Communication.FlowInactive",
        "Communication flow is currently inactive and cannot enroll new recipients."
    );

    public static readonly DomainError SystemFlowProtected = DomainError.Conflict(
        "Communication.SystemFlowProtected",
        "Built-in system communication flows cannot be deleted."
    );

    public static readonly DomainError FlowHasInstances = DomainError.Conflict(
        "Communication.FlowHasInstances",
        "Communication flow has historical instances and cannot be deleted; deactivate it instead."
    );

    public static readonly DomainError FlowStepsInvalid = DomainError.Validation(
        "Communication.FlowStepsInvalid",
        "Communication flow must define 1–25 valid steps with unique step keys, non-negative delays, and channel templates."
    );

    public static readonly DomainError FlowConversionConfigInvalid = DomainError.Validation(
        "Communication.FlowConversionConfigInvalid",
        "Conversion window must be 1–365 days, JoinedTargetGroup requires a target group ID, and ClickedGoalUrl requires a URL prefix."
    );

    public static readonly DomainError FlowInstanceNotFound = DomainError.NotFound(
        "Communication.FlowInstanceNotFound",
        "Communication flow instance was not found."
    );

    public static readonly DomainError FlowRecipientNotFound = DomainError.NotFound(
        "Communication.FlowRecipientNotFound",
        "Communication flow recipient enrollment was not found."
    );

    public static readonly DomainError FlowEnrollmentEmpty = DomainError.Validation(
        "Communication.FlowEnrollmentEmpty",
        "At least one recipient with a valid email address or E.164 phone number is required to enroll in a flow."
    );
}
