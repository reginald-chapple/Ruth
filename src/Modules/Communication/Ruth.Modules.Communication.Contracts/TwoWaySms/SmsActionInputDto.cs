namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Input specification for an SmsAction rule inside an SmsPipeline.</summary>
/// <param name="Name">Descriptive action name.</param>
/// <param name="Order">Zero-based execution order within the pipeline.</param>
/// <param name="MatchMode">Keyword matching strategy.</param>
/// <param name="Keyword">Keyword token (required unless <see cref="SmsKeywordMatchMode.FallbackAny" />).</param>
/// <param name="ActionKind">Automated behavior to run on match.</param>
/// <param name="ReplyTemplate">Optional SMS reply merge template.</param>
/// <param name="TargetSystemCommunicationKey">Optional system communication key when <see cref="SmsActionKind.PublishSystemNotification" />.</param>
/// <param name="ContinueProcessing">Whether subsequent actions in the pipeline continue executing after a match.</param>
/// <param name="IsActive">Whether this action rule is active.</param>
public sealed record SmsActionInputDto(
    string Name,
    int Order,
    SmsKeywordMatchMode MatchMode,
    string? Keyword,
    SmsActionKind ActionKind,
    string? ReplyTemplate = null,
    string? TargetSystemCommunicationKey = null,
    bool ContinueProcessing = false,
    bool IsActive = true
);
